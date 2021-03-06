using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Flame;
using Flame.Clr;
using Flame.Llvm;
using Flame.TypeSystem;
using LLVMSharp;
using ManagedCuda;
using ManagedCuda.BasicTypes;

namespace Turbo
{
    /// <summary>
    /// Encodes objects in CUDA device memory.
    /// </summary>
    internal class CudaEncoder : ObjectEncoder<CudaEncoder.ValueOrRefObject, CudaEncoder.MirrorBufferPtr>, IDisposable
    {
        public CudaEncoder(CudaModule module)
            : base(module.IntermediateModule, module.TargetData)
        {
            this.Assembly = module.SourceAssembly;
            this.PtxModule = module.CompiledModule;
            this.Context = module.Context;
            this.pendingBuffers = new List<MirrorBufferPtr>();
            this.allocatedBuffers = new List<CUdeviceptr>();
        }

        /// <summary>
        /// Gets the CLR assembly from which the PTX module is generated.
        /// </summary>
        /// <value>A CLR assembly.</value>
        public ClrAssembly Assembly { get; private set; }

        /// <summary>
        /// Gets the PTX module generated for the kernel.
        /// </summary>
        /// <value>A PTX module.</value>
        public CUmodule PtxModule { get; private set; }

        /// <summary>
        /// Gets the CUDA context to use.
        /// </summary>
        /// <value>A CUDA context.</value>
        public CudaContext Context { get; private set; }

        /// <summary>
        /// A list of (device buffer, host buffer, size) triples. The host buffers
        /// in this list are populated and their contents are then copied to their
        /// corresponding device buffers, after which the host buffers are deleted.
        /// </summary>
        private List<MirrorBufferPtr> pendingBuffers;

        /// <summary>
        /// A list of all device memory buffers allocated by this encoder.
        /// </summary>
        private List<CUdeviceptr> allocatedBuffers;

        /// <summary>
        /// Encodes a single object as an in-memory object in CUDA device memory.
        /// Memory used for representing the encoded object is managed by the encoder.
        /// Dispose the encoder to free the memory backing the encoded object.
        /// </summary>
        /// <param name="value">The value to encode.</param>
        /// <returns>A pointer to the object's encoded value.</returns>
        public CUdeviceptr Encode(object value)
        {
            if (value == null)
            {
                return new CUdeviceptr();
            }

            var obj = ValueOrRefObject.Reference(value);
            var ptr = EncodeBoxPointer(obj, ((PointerType)TypeOf(obj)).ElementType);
            foreach (var buf in pendingBuffers)
            {
                Context.CopyToDevice(buf.DeviceBuffer, buf.HostBuffer, buf.Size);
                Marshal.FreeHGlobal(buf.HostBuffer);
            }
            pendingBuffers = new List<MirrorBufferPtr>();
            return ptr.DeviceBuffer;
        }

        public override MirrorBufferPtr AllocateBuffer(int size)
        {
            var dev = Context.AllocateMemory(size);
            allocatedBuffers.Add(dev);
            var host = Marshal.AllocHGlobal(size);
            var result = new MirrorBufferPtr(dev, host, size);
            pendingBuffers.Add(result);
            return result;
        }

        public override MirrorBufferPtr IndexPointer(MirrorBufferPtr pointer, int offset)
        {
            return new MirrorBufferPtr(pointer.DeviceBuffer + offset, pointer.HostBuffer + offset, pointer.Size - offset);
        }

        public override void EncodePointer(MirrorBufferPtr address, MirrorBufferPtr value)
        {
            Marshal.WriteIntPtr(address.HostBuffer, value.DeviceBuffer.Pointer);
        }

        public override MirrorBufferPtr GetGlobalAddress(LLVMValueRef value)
        {
            SizeT size;
            var ptr = GetGlobalAddress(value, PtxModule, out size);
            return new MirrorBufferPtr(ptr, IntPtr.Zero, size);
        }

        public static CUdeviceptr GetGlobalAddress(LLVMValueRef value, CUmodule module, out SizeT size)
        {
            if (value.IsAConstantExpr().Pointer != IntPtr.Zero && value.GetConstOpcode() == LLVMOpcode.LLVMBitCast)
            {
                return GetGlobalAddress(value.GetOperand(0), module, out size);
            }

            var ptr = new CUdeviceptr();
            size = new SizeT();
            var result = ManagedCuda.DriverAPINativeMethods.ModuleManagement.cuModuleGetGlobal_v2(
                ref ptr, ref size, module, GetGlobalName(value));

            if (result == ManagedCuda.BasicTypes.CUResult.Success)
            {
                return ptr;
            }
            else
            {
                throw new CudaException(result);
            }
        }

        private static string GetGlobalName(LLVMValueRef value)
        {
            // TODO: this is a super convoluted way to get a global's name.
            // Surely there ought to be some direct API?
            var nameAndAssignment = value.GetValueName();
            var assignmentIndex = nameAndAssignment.LastIndexOf('=');
            var name = nameAndAssignment;
            if (assignmentIndex > 0)
            {
                name = nameAndAssignment.Substring(0, assignmentIndex).TrimEnd();
            }
            if (name[0] == '@')
            {
                name = name.Substring(1);
            }

            return name;
        }

        public override ValueOrRefObject LoadBoxPointer(ValueOrRefObject pointer)
        {
            return ValueOrRefObject.Value(pointer.Object);
        }

        public override bool TryEncodePrimitive(ValueOrRefObject value, MirrorBufferPtr buffer)
        {
            var val = value.Object;
            if (val == null)
            {
                Marshal.WriteIntPtr(buffer.HostBuffer, IntPtr.Zero);
                return true;
            }
            else if (val is IntPtr)
            {
                Marshal.WriteIntPtr(buffer.HostBuffer, (IntPtr)val);
                return true;
            }
            else if (val is byte)
            {
                Marshal.WriteByte(buffer.HostBuffer, (byte)val);
                return true;
            }
            else if (val is sbyte)
            {
                Marshal.WriteByte(buffer.HostBuffer, (byte)(sbyte)val);
                return true;
            }
            else if (val is short)
            {
                Marshal.WriteInt16(buffer.HostBuffer, (short)val);
                return true;
            }
            else if (val is ushort)
            {
                Marshal.WriteInt16(buffer.HostBuffer, (short)(ushort)val);
                return true;
            }
            else if (val is int)
            {
                Marshal.WriteInt32(buffer.HostBuffer, (int)val);
                return true;
            }
            else if (val is uint)
            {
                Marshal.WriteInt32(buffer.HostBuffer, (int)(uint)val);
                return true;
            }
            else if (val is long)
            {
                Marshal.WriteInt64(buffer.HostBuffer, (long)val);
                return true;
            }
            else if (val is ulong)
            {
                Marshal.WriteInt64(buffer.HostBuffer, (long)(ulong)val);
                return true;
            }
            // TODO: handle other primitive types, e.g., floating-point numbers and delegates.
            else if (val is Array)
            {
                var arr = (Array)val;
                var arrType = val.GetType();
                var elemType = ToClr(arrType.GetElementType());
                bool refElements = false;
                if (elemType.IsReferenceType())
                {
                    elemType = elemType.MakePointerType(PointerKind.Box);
                    refElements = true;
                }
                var elemSize = SizeOf(elemType);
                var totalElements = arr.Length;

                // Allocate a buffer for the array.
                var buf = AllocateBuffer(MetadataSize + arr.Rank * 8 + totalElements * elemSize);

                // Initialize the array's vtable.
                InitializeObject(buf, ToClr(arrType));
                buf = IndexPointer(buf, MetadataSize);

                // Map the array to its encoded version.
                RegisterEncoded(value, buf);
                EncodePointer(buffer, buf);

                // Encode array dimensions.
                // FIXME: this logic is directly dependent on the GC interface. Can we abstract
                // over this in a reasonable way at the Flame.Llvm level?
                for (int i = 0; i < arr.Rank; i++)
                {
                    Marshal.WriteInt64(buf.HostBuffer, arr.GetLongLength(i));
                    buf = IndexPointer(buf, 8);
                }

                // Encode array contents.
                foreach (var item in arr)
                {
                    var elem = refElements ? ValueOrRefObject.Reference(item) : ValueOrRefObject.Value(item);
                    Encode(elem, buf);
                    buf = IndexPointer(buf, elemSize);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private IType ToClr(Type type)
        {
            return Assembly.Resolve(
                Assembly.Definition.MainModule.ImportReference(
                    type));
        }

        public override IType TypeOf(ValueOrRefObject value)
        {
            var type = ToClr(value.Object.GetType());

            if (value.IsReference)
            {
                return type.MakePointerType(PointerKind.Box);
            }
            else
            {
                return type;
            }
        }

        public override IReadOnlyDictionary<IField, ValueOrRefObject> GetFieldValues(ValueOrRefObject value)
        {
            var t = value.Object.GetType();
            var results = new Dictionary<IField, ValueOrRefObject>();
            foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var flameField = Assembly.Resolve(Assembly.Definition.MainModule.ImportReference(field));
                var val = field.GetValue(value.Object);
                results[flameField] = flameField.FieldType.IsPointerType(PointerKind.Box)
                    ? ValueOrRefObject.Reference(val)
                    : ValueOrRefObject.Value(val);
            }
            return results;
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

                foreach (var fatPtr in pendingBuffers)
                {
                    Marshal.FreeHGlobal(fatPtr.HostBuffer);
                }
                foreach (var buf in allocatedBuffers)
                {
                    Context.FreeMemory(buf);
                }

                disposedValue = true;
            }
        }

        ~CudaEncoder()
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

        /// <summary>
        /// A pointer to a buffer that has been allocated by both the device
        /// and the host. The host will populate the buffer and then copy it
        /// to the device.
        /// </summary>
        public struct MirrorBufferPtr
        {
            public MirrorBufferPtr(
                CUdeviceptr deviceBuffer,
                IntPtr hostBuffer,
                int size)
            {
                this.DeviceBuffer = deviceBuffer;
                this.HostBuffer = hostBuffer;
                this.Size = size;
            }

            public CUdeviceptr DeviceBuffer { get; private set; }

            public IntPtr HostBuffer { get; private set; }

            public int Size { get; private set; }
        }

        public struct ValueOrRefObject
        {
            private ValueOrRefObject(object @object, bool isValue)
            {
                this.Object = @object;
                this.IsValue = isValue;
            }

            /// <summary>
            /// Gets the object managed by this struct.
            /// </summary>
            /// <value>An object.</value>
            public object Object { get; private set; }

            /// <summary>
            /// Tells if this struct is to be interpreted as the value of
            /// <see cref="Object"/> rather than a reference to it.
            /// </summary>
            /// <value>A Boolean flag.</value>
            public bool IsValue { get; private set; }

            /// <summary>
            /// Tells if this struct is to be interpreted as a reference to
            /// <see cref="Object"/> rather than its value.
            /// </summary>
            /// <value>A Boolean flag.</value>
            public bool IsReference => !IsValue;

            public override string ToString()
            {
                return $"{Object} [{(IsValue ? "value" : "ref")}]";
            }

            public static ValueOrRefObject Value(object obj)
            {
                return new ValueOrRefObject(obj, true);
            }

            public static ValueOrRefObject Reference(object obj)
            {
                return new ValueOrRefObject(obj, false);
            }
        }
    }
}
