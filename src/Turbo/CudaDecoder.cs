using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Flame;
using Flame.Clr;
using Flame.Llvm;
using Flame.Llvm.Emit;
using Flame.TypeSystem;
using LLVMSharp;
using ManagedCuda;
using ManagedCuda.BasicTypes;

namespace Turbo
{
    /// <summary>
    /// Decodes objects in CUDA device memory to CLR objects.
    /// </summary>
    internal class CudaDecoder : ObjectDecoder<object, CudaDecoder.BoxPointer>, IDisposable
    {
        public CudaDecoder(
            CudaModule module,
            IReadOnlyDictionary<CUdeviceptr, object> existingObjects)
            : base(
                module.IntermediateModule,
                module.TargetData,
                existingObjects.ToDictionary(p => new BoxPointer(p.Key, 0), p => p.Value))
        {
            this.TypeSystem = module.SourceAssembly.Resolver.TypeEnvironment;
            this.Context = module.Context;

            this.downloadedObjects = new Dictionary<CUdeviceptr, DownloadedBox>();
            this.metadataToType = new Dictionary<CUdeviceptr, IType>();

            // Map metadata to types.
            foreach (var type in module.IntermediateModule.Metadata.TypesWithMetadata)
            {
                var meta = module.IntermediateModule.Metadata.GetMetadata(type, module.IntermediateModule);
                SizeT size;
                var addr = CudaEncoder.GetGlobalAddress(meta, module.CompiledModule, out size);
                metadataToType[addr] = type;
            }
        }

        public TypeEnvironment TypeSystem { get; private set; }

        public CudaContext Context { get; private set; }

        private Dictionary<CUdeviceptr, DownloadedBox> downloadedObjects;

        private Dictionary<CUdeviceptr, IType> metadataToType;

        /// <summary>
        /// Decodes an object stored at a particular address.
        /// </summary>
        /// <param name="pointer">A pointer to an object to decode.</param>
        /// <returns>A decoded object.</returns>
        public object Decode(CUdeviceptr pointer)
        {
            return Decode(DownloadBox(pointer));
        }

        public override BoxPointer IndexPointer(BoxPointer pointer, int offset)
        {
            return new BoxPointer(pointer.BasePointer, pointer.Offset + offset);
        }

        public override BoxPointer LoadBoxPointer(BoxPointer pointer)
        {
            var basePtr = (CUdeviceptr)(SizeT)Marshal.ReadIntPtr(pointer.ToIntPtr(this));
            return DownloadBox(basePtr);
        }

        private unsafe BoxPointer DownloadBox(CUdeviceptr basePtr)
        {
            if (basePtr == 0 || downloadedObjects.ContainsKey(basePtr))
            {
                return new BoxPointer(basePtr, 0);
            }

            // Copy metadata from device memory.
            CUdeviceptr metaPtr;
            Context.CopyToHost((IntPtr)(&metaPtr), basePtr - MetadataSize, (uint)MetadataSize);

            // Determine the object's type.
            var type = metadataToType[metaPtr];

            // Download the object from GPU memory.
            var size = SizeOfObject(basePtr, type);
            var data = Marshal.AllocHGlobal(size);
            // TODO: download whole array if we're dealing with an array type.
            Context.CopyToHost(data, basePtr, (uint)size);

            // Keep track of the object's type and data.
            downloadedObjects[basePtr] = new DownloadedBox(type, data);

            // Return a fat pointer.
            return new BoxPointer(basePtr, 0);
        }

        private int SizeOfObject(CUdeviceptr basePtr, IType type)
        {
            int rank;
            if (ClrArrayType.TryGetArrayRank(type, out rank))
            {
                var dims = new long[rank];
                Context.CopyToHost(dims, basePtr);

                IType elementType;
                ClrArrayType.TryGetArrayElementType(type, out elementType);
                return sizeof(long) * rank + dims.Aggregate(SizeOf(elementType), (x, y) => x * (int)y);
            }
            else
            {
                return SizeOf(type);
            }
        }

        public override IType TypeOf(BoxPointer pointer)
        {
            return pointer.TypeOf(this);
        }

        private static Type ToClr(IType type)
        {
            if (type.IsPointerType(PointerKind.Box))
            {
                return ToClr(((PointerType)type).ElementType);
            }
            else if (type.IsPointerType(PointerKind.Transient))
            {
                return ToClr(((PointerType)type).ElementType).MakePointerType();
            }
            else if (type.IsPointerType(PointerKind.Reference))
            {
                return ToClr(((PointerType)type).ElementType).MakeByRefType();
            }
            else if (type.IsRecursiveGenericInstance())
            {
                var genArgs = type.GetRecursiveGenericArguments();
                var genDecl = type.GetRecursiveGenericDeclaration();
                return ToClr(genDecl).MakeGenericType(genArgs.Select(ToClr).ToArray());
            }
            else if (ClrArrayType.IsArrayType(type))
            {
                IType elementType;
                ClrArrayType.TryGetArrayElementType(type, out elementType);
                int rank;
                ClrArrayType.TryGetArrayRank(type, out rank);
                var clrElementType = ToClr(elementType);
                return rank == 1 ? clrElementType.MakeArrayType() : clrElementType.MakeArrayType(rank);
            }
            else
            {
                var clrType = (ClrTypeDefinition)type;
                if (type.Parent.IsType)
                {
                    var parent = ToClr(type.Parent.Type);
                    return parent.GetNestedType(clrType.Definition.Name, BindingFlags.Public | BindingFlags.NonPublic);
                }
                else
                {
                    // TODO: handle special types: delegates.
                    return Type.GetType($"{clrType.Definition.FullName},{clrType.Definition.Module.Assembly.FullName}", true);
                }
            }
        }

        private static FieldInfo ToClr(IField field)
        {
            var type = ToClr(field.ParentType);
            var genDecl = (ClrFieldDefinition)field.GetRecursiveGenericDeclaration();
            return type.GetField(genDecl.Definition.Name);
        }

        public override object CreateObject(IType type)
        {
            return FormatterServices.GetUninitializedObject(ToClr(type));
        }

        public override void SetField(object obj, IField field, object value)
        {
            ToClr(field).SetValue(obj, value);
        }

        public override bool TryDecodePrimitive(BoxPointer pointer, IType type, out object obj)
        {
            if (pointer.BasePointer == 0)
            {
                obj = null;
                return true;
            }
            else if (type == TypeSystem.Int8)
            {
                obj = (sbyte)Marshal.ReadByte(pointer.ToIntPtr(this));
                return true;
            }
            else if (type == TypeSystem.UInt8)
            {
                obj = Marshal.ReadByte(pointer.ToIntPtr(this));
                return true;
            }
            else if (type == TypeSystem.Int16)
            {
                obj = Marshal.ReadInt16(pointer.ToIntPtr(this));
                return true;
            }
            else if (type == TypeSystem.UInt16)
            {
                obj = (ushort)Marshal.ReadInt16(pointer.ToIntPtr(this));
                return true;
            }
            else if (type == TypeSystem.Char)
            {
                obj = (char)Marshal.ReadInt16(pointer.ToIntPtr(this));
                return true;
            }
            else if (type == TypeSystem.Int32)
            {
                obj = Marshal.ReadInt32(pointer.ToIntPtr(this));
                return true;
            }
            else if (type == TypeSystem.UInt32)
            {
                obj = (uint)Marshal.ReadInt32(pointer.ToIntPtr(this));
                return true;
            }
            else if (type == TypeSystem.Int64)
            {
                obj = Marshal.ReadInt64(pointer.ToIntPtr(this));
                return true;
            }
            else if (type == TypeSystem.UInt64)
            {
                obj = (ulong)Marshal.ReadInt64(pointer.ToIntPtr(this));
                return true;
            }
            else if (type == TypeSystem.NaturalInt)
            {
                obj = Marshal.ReadIntPtr(pointer.ToIntPtr(this));
                return true;
            }
            else if (type == TypeSystem.NaturalUInt
                || type == TypeSystem.Float32
                || type == TypeSystem.Float64)
            {
                throw new System.NotImplementedException();
            }
            else if (ClrArrayType.IsArrayType(type))
            {
                int rank;
                ClrArrayType.TryGetArrayRank(type, out rank);
                IType elementType;
                ClrArrayType.TryGetArrayElementType(type, out elementType);

                // Decode array dimensions.
                var ptr = pointer.ToIntPtr(this);
                var dims = new long[rank];
                for (int i = 0; i < rank; i++)
                {
                    dims[i] = Marshal.ReadInt64(ptr);
                    ptr += sizeof(long);
                }

                // Create a host array.
                var arr = Array.CreateInstance(ToClr(type), dims);
                obj = arr;
                RegisterDecoded(pointer, arr);

                // Decode array contents.
                DecodeArrayContents(pointer, elementType, dims, arr);
                return true;
            }
            else
            {
                // TODO: handle delegates.
                obj = null;
                return false;
            }
        }

        private void DecodeArrayContents(
            BoxPointer basePointer,
            IType elementType,
            long[] dims,
            Array array)
        {
            var elementSize = SizeOf(elementType);
            var rank = dims.Length;

            basePointer = IndexPointer(basePointer, rank * 8);

            var index = new long[rank];
            bool done = false;
            while (true)
            {
                for (int i = rank - 1; i >= 0; i--)
                {
                    if (index[i] >= dims[i])
                    {
                        if (i == 0)
                        {
                            done = true;
                        }
                        else
                        {
                            index[i] = 0;
                            index[i - 1]++;
                        }
                    }
                }
                if (done)
                {
                    break;
                }

                array.SetValue(DecodeFieldlike(elementType, basePointer), index);
                index[rank - 1]++;
                basePointer = IndexPointer(basePointer, elementSize);
            }
        }

        public override bool TryUpdatePrimitive(BoxPointer pointer, object obj, IType type)
        {
            if (obj is sbyte || obj is byte
                || obj is short || obj is ushort || obj is char
                || obj is int || obj is uint
                || obj is long || obj is ulong
                || obj is float || obj is double
                || obj is IntPtr || obj is UIntPtr)
            {
                // These objects are immutable. They never need to be updated.
                return true;
            }
            else if (obj is Array)
            {
                IType elementType;
                ClrArrayType.TryGetArrayElementType(type, out elementType);
                var array = (Array)obj;
                DecodeArrayContents(
                    pointer,
                    elementType,
                    Enumerable.Range(0, array.Rank).Select(array.GetLongLength).ToArray(),
                    array);
                return true;
            }
            else
            {
                // TODO: handle delegates, etc.
                return false;
            }
        }

        internal struct BoxPointer
        {
            public BoxPointer(CUdeviceptr basePointer, int offset)
            {
                this.BasePointer = basePointer;
                this.Offset = offset;
            }

            public CUdeviceptr BasePointer { get; private set; }
            public int Offset { get; private set; }

            public IType TypeOf(CudaDecoder decoder)
            {
                if (BasePointer == 0)
                {
                    return null;
                }

                return decoder.downloadedObjects[BasePointer].Type;
            }

            public IntPtr ToIntPtr(CudaDecoder decoder)
            {
                return decoder.downloadedObjects[BasePointer].BasePointer + Offset;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                // if (disposing)
                // {
                //     // TODO: dispose managed state (managed objects).
                // }

                foreach (var pair in downloadedObjects)
                {
                    Marshal.FreeHGlobal(pair.Value.BasePointer);
                }

                disposedValue = true;
            }
        }

        ~CudaDecoder()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        internal struct DownloadedBox
        {
            public DownloadedBox(IType type, IntPtr basePointer)
            {
                this.Type = type;
                this.BasePointer = basePointer;
            }

            public IType Type { get; private set; }
            public IntPtr BasePointer { get; private set; }
        }
    }
}
