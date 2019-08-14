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

        private BoxPointer DownloadBox(CUdeviceptr basePtr)
        {
            if (downloadedObjects.ContainsKey(basePtr))
            {
                return new BoxPointer(basePtr, 0);
            }

            // Copy metadata from device memory.
            // TODO: put this on the stack instead? Using an array as a temporary
            // costs us one allocation per pointer load.
            var metaBuf = new IntPtr[1];
            Context.CopyToHost(metaBuf, basePtr - MetadataSize);
            var metaPtr = (CUdeviceptr)(SizeT)metaBuf[0];

            // Determine the object's type.
            var type = metadataToType[metaPtr];

            // Download the object from GPU memory.
            var size = SizeOf(type);
            var data = Marshal.AllocHGlobal(size);
            Context.CopyToHost(data, basePtr, (uint)size);

            // Keep track of the object's type and data.
            downloadedObjects[basePtr] = new DownloadedBox(type, data);

            // Return a fat pointer.
            return new BoxPointer(basePtr, 0);
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
            else
            {
                // TODO: handle special types: arrays and delegates.
                var clrType = (ClrTypeDefinition)type;
                return Type.GetType(clrType.Definition.FullName);
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
            if (type == TypeSystem.Int8)
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
            else
            {
                // TODO: handle delegates, arrays, etc.
                obj = null;
                return false;
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
            else
            {
                // TODO: handle delegates, arrays, etc.
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
