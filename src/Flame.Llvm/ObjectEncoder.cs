using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Flame.Llvm.Emit;
using Flame.TypeSystem;
using LLVMSharp;

namespace Flame.Llvm
{
    /// <summary>
    /// Encodes and/or decodes in-memory objects.
    /// </summary>
    /// <typeparam name="T">The type of a decoded object.</typeparam>
    public abstract class ObjectMarshal<T>
    {
        public ObjectMarshal(ModuleBuilder compiledModule, LLVMTargetDataRef target)
        {
            this.CompiledModule = compiledModule;
            this.Target = target;
        }

        /// <summary>
        /// Gets the compiled module whose in-memory representation is to be used
        /// for encoding objects.
        /// </summary>
        /// <value>A compiled module.</value>
        public ModuleBuilder CompiledModule { get; private set; }

        /// <summary>
        /// Gets the target data to use for determining field offsets and type sizes.
        /// </summary>
        /// <value>LLVM target data.</value>
        public LLVMTargetDataRef Target { get; private set; }

        /// <summary>
        /// Gets a type's encoded size in bytes.
        /// </summary>
        /// <param name="type">A type.</param>
        /// <returns>The type's size in bytes.</returns>
        public int SizeOf(IType type)
        {
            return (int)LLVM.StoreSizeOfType(Target, CompiledModule.ImportType(type)) / 8;
        }

        /// <summary>
        /// Gets a type's encoded size in bytes.
        /// </summary>
        /// <param name="type">A type.</param>
        /// <param name="metadataSize">The object's metadata size.</param>
        /// <returns>The type's size in bytes, including the size of the metadata.</returns>
        public int SizeOfWithMetadata(IType type, out int metadataSize)
        {
            var ext = GCInterface.GetMetadataExtendedType(CompiledModule.ImportType(type), CompiledModule);
            metadataSize = (int)LLVM.OffsetOfElement(Target, ext, 1);
            return (int)LLVM.StoreSizeOfType(Target, ext) / 8;
        }

        /// <summary>
        /// Gets the offset of a particular field.
        /// </summary>
        /// <param name="field">A non-static field.</param>
        /// <returns>The field's offset in its parent type's layout.</returns>
        public int GetFieldOffset(IField field)
        {
            var parent = CompiledModule.ImportType(field.ParentType);
            var index = CompiledModule.GetFieldIndex(field);
            return (int)LLVM.OffsetOfElement(Target, parent, (uint)index);
        }
    }

    /// <summary>
    /// Encodes in-memory objects.
    /// </summary>
    /// <typeparam name="T">The type of a decoded object.</typeparam>
    public abstract class ObjectEncoder<T> : ObjectMarshal<T>
    {
        public ObjectEncoder(ModuleBuilder compiledModule, LLVMTargetDataRef target)
            : base(compiledModule, target)
        {
            this.encoded = new Dictionary<T, IntPtr>();
        }

        /// <summary>
        /// A mapping of box pointers to their encoded versions.
        /// </summary>
        private Dictionary<T, IntPtr> encoded;

        /// <summary>
        /// Gets the type of a particular value.
        /// </summary>
        /// <param name="value">The type of a value.</param>
        /// <returns>A type.</returns>
        public abstract IType TypeOf(T value);

        /// <summary>
        /// Allocates a GC-managed buffer of a particular size.
        /// </summary>
        /// <param name="size">The size of the buffer to allocate.</param>
        /// <returns>A pointer to the buffer.</returns>
        public abstract IntPtr AllocateBuffer(int size);

        /// <summary>
        /// Loads the value stored at a particular pointer.
        /// </summary>
        /// <param name="pointer">The pointer whose value is to be loaded.</param>
        /// <returns>The value stored at the pointer.</returns>
        public abstract T LoadPointer(T pointer);

        /// <summary>
        /// Gets the address of a global variable in memory.
        /// </summary>
        /// <param name="value">The global variable to query.</param>
        /// <returns>An address to <paramref name="value"/>.</returns>
        public abstract IntPtr GetGlobalAddress(LLVMValueRef value);

        /// <summary>
        /// Gets a field's value.
        /// </summary>
        /// <param name="field">A non-static field.</param>
        /// <param name="value">An object that includes <paramref name="field"/> in its layout.</param>
        /// <returns><paramref name="value"/>'s value for <paramref name="field"/>.</returns>
        public abstract T GetFieldValue(IField field, T value);

        /// <summary>
        /// Stores a pointer at a particular address.
        /// </summary>
        /// <param name="address">An address to write <paramref name="value"/> to.</param>
        /// <param name="value">A value to write to <paramref name="address"/>.</param>
        public virtual void EncodeIntPtr(IntPtr address, IntPtr value)
        {
            Marshal.WriteIntPtr(address, value);
        }

        /// <summary>
        /// Tries to encode a primitive value.
        /// </summary>
        /// <param name="value">The primitive value to encode.</param>
        /// <param name="buffer">A buffer to write <paramref name="value"/>'s encoded version to.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> is a primitive and has been encoded; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool TryEncodePrimitive(T value, IntPtr buffer);

        /// <summary>
        /// Initializes an object at a particular address.
        /// </summary>
        /// <param name="address">The object to initialize.</param>
        /// <param name="type">The type of the object to initialize.</param>
        public void InitializeObject(IntPtr address, IType type)
        {
            EncodeIntPtr(address, ConstToIntPtr(CompiledModule.Metadata.GetMetadata(type, CompiledModule)));
        }

        private IntPtr ConstToIntPtr(LLVMValueRef value)
        {
            if (value.IsABitCastInst().Pointer == value.Pointer)
            {
                return ConstToIntPtr(value.GetOperand(0));
            }
            else if (value.IsAGlobalVariable().Pointer == value.Pointer)
            {
                return GetGlobalAddress(value);
            }
            else
            {
                throw new InvalidOperationException(
                    string.Format("Cannot convert '{0}' to a pointer constant.", value));
            }
        }

        /// <summary>
        /// Creates an object of a particular type.
        /// </summary>
        /// <param name="type">The object to create.</param>
        /// <returns>An address to the object.</returns>
        public IntPtr CreateObject(IType type)
        {
            int metaSize;
            var address = AllocateBuffer(SizeOfWithMetadata(type, out metaSize));
            InitializeObject(address, type);
            return address + metaSize;
        }

        /// <summary>
        /// Encodes a particular value and writes its encoded
        /// representation to a buffer.
        /// </summary>
        /// <param name="value">A value to encode.</param>
        /// <param name="buffer">
        /// A buffer to write <paramref name="value"/>'s encoded representation to.
        /// </param>
        public void Encode(T value, IntPtr buffer)
        {
            if (TryEncodePrimitive(value, buffer))
            {
                return;
            }

            var type = TypeOf(value);

            if (type.IsPointerType(PointerKind.Box))
            {
                var boxPtrType = (PointerType)type;
                var address = EncodeBoxPointer(value, boxPtrType.ElementType);
                EncodeIntPtr(buffer, address);
            }
            else
            {
                EncodeStruct(value, type, buffer);
            }
        }

        private void EncodeStruct(T value, IType type, IntPtr buffer)
        {
            foreach (var field in type.Fields)
            {
                Encode(GetFieldValue(field, value), buffer + GetFieldOffset(field));
            }
        }

        protected IntPtr EncodeBoxPointer(T box, IType elementType)
        {
            IntPtr address;
            if (!encoded.TryGetValue(box, out address))
            {
                address = CreateObject(elementType);
                Encode(LoadPointer(box), address);
            }
            return address;
        }
    }
}
