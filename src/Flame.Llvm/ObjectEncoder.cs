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
        /// <summary>
        /// Creates an object marshal.
        /// </summary>
        /// <param name="compiledModule">
        /// A compiled LLVM module to inspect for type data layouts.
        /// </param>
        /// <param name="target">
        /// A target data layout to use for determining the precise layout of data types.
        /// </param>
        public ObjectMarshal(ModuleBuilder compiledModule, LLVMTargetDataRef target)
        {
            this.CompiledModule = compiledModule;
            this.Target = target;

            var ext = GCInterface.GetMetadataExtendedType(
                LLVM.StructTypeInContext(CompiledModule.Context, new LLVMTypeRef[] { }, false),
                CompiledModule);
            MetadataSize = (int)LLVM.OffsetOfElement(Target, ext, 1);
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
        /// Gets the size of an object's metadata prefix, in bytes.
        /// </summary>
        /// <value>The size in bytes of an object's metadata.</value>
        public int MetadataSize { get; private set; }

        /// <summary>
        /// Gets a type's encoded size in bytes.
        /// </summary>
        /// <param name="type">A type.</param>
        /// <returns>The type's size in bytes.</returns>
        public int SizeOf(IType type)
        {
            return (int)LLVM.StoreSizeOfType(Target, CompiledModule.ImportType(type));
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
            return (int)LLVM.StoreSizeOfType(Target, ext);
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
    /// <typeparam name="TObj">The type of a decoded object.</typeparam>
    /// <typeparam name="TPtr">The type of a pointer to an encoded object.</typeparam>
    public abstract class ObjectEncoder<TObj, TPtr> : ObjectMarshal<TObj>
    {
        /// <summary>
        /// Creates an object encoder.
        /// </summary>
        /// <param name="compiledModule">
        /// A compiled LLVM module to inspect for type data layouts.
        /// </param>
        /// <param name="target">
        /// A target data layout to use for determining the precise layout of data types.
        /// </param>
        public ObjectEncoder(ModuleBuilder compiledModule, LLVMTargetDataRef target)
            : base(compiledModule, target)
        {
            this.encoded = new Dictionary<TObj, TPtr>();
        }

        /// <summary>
        /// A mapping of box pointers to their encoded versions.
        /// </summary>
        private Dictionary<TObj, TPtr> encoded;

        /// <summary>
        /// Gets a mapping of objects to their encoded versions.
        /// </summary>
        /// <value>A mapping of objects to their encoded versions.</value>
        public IReadOnlyDictionary<TObj, TPtr> EncodedObjects => encoded;

        /// <summary>
        /// Gets the type of a particular value.
        /// </summary>
        /// <param name="value">The type of a value.</param>
        /// <returns>A type.</returns>
        public abstract IType TypeOf(TObj value);

        /// <summary>
        /// Allocates a GC-managed buffer of a particular size.
        /// </summary>
        /// <param name="size">The size of the buffer to allocate.</param>
        /// <returns>A pointer to the buffer.</returns>
        public abstract TPtr AllocateBuffer(int size);

        /// <summary>
        /// Loads the value stored at a particular box pointer.
        /// </summary>
        /// <param name="pointer">The pointer whose value is to be loaded.</param>
        /// <returns>The value stored at the pointer.</returns>
        public abstract TObj LoadBoxPointer(TObj pointer);

        /// <summary>
        /// Adds an offset to a pointer.
        /// </summary>
        /// <param name="pointer">A base pointer.</param>
        /// <param name="offset">An offset to add to <paramref name="pointer"/>.</param>
        /// <returns>A modified pointer.</returns>
        public abstract TPtr IndexPointer(TPtr pointer, int offset);

        /// <summary>
        /// Gets the address of a global variable in memory.
        /// </summary>
        /// <param name="value">The global variable to query.</param>
        /// <returns>An address to <paramref name="value"/>.</returns>
        public abstract TPtr GetGlobalAddress(LLVMValueRef value);

        /// <summary>
        /// Gets a mapping of an object's fields to their values..
        /// </summary>
        /// <param name="value">An object.</param>
        /// <returns>A mapping of <paramref name="value"/>'s fields to those fields' values.</returns>
        public abstract IReadOnlyDictionary<IField, TObj> GetFieldValues(TObj value);

        /// <summary>
        /// Stores a pointer at a particular address.
        /// </summary>
        /// <param name="address">An address to write <paramref name="value"/> to.</param>
        /// <param name="value">A value to write to <paramref name="address"/>.</param>
        public abstract void EncodePointer(TPtr address, TPtr value);

        /// <summary>
        /// Tries to encode a primitive value.
        /// </summary>
        /// <param name="value">The primitive value to encode.</param>
        /// <param name="buffer">A buffer to write <paramref name="value"/>'s encoded version to.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="value"/> is a primitive and has been encoded; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool TryEncodePrimitive(TObj value, TPtr buffer);

        /// <summary>
        /// Initializes an object at a particular address.
        /// </summary>
        /// <param name="address">The object to initialize.</param>
        /// <param name="type">The type of the object to initialize.</param>
        public void InitializeObject(TPtr address, IType type)
        {
            EncodePointer(address, ConstToPtr(CompiledModule.Metadata.GetMetadata(type, CompiledModule)));
        }

        private TPtr ConstToPtr(LLVMValueRef value)
        {
            if (value.IsAConstantExpr().Pointer != IntPtr.Zero && value.GetConstOpcode() == LLVMOpcode.LLVMBitCast)
            {
                return ConstToPtr(value.GetOperand(0));
            }
            else if (value.IsAGlobalVariable().Pointer != IntPtr.Zero)
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
        public TPtr CreateObject(IType type)
        {
            int metaSize;
            var address = AllocateBuffer(SizeOfWithMetadata(type, out metaSize));
            InitializeObject(address, type);
            return IndexPointer(address, metaSize);
        }

        /// <summary>
        /// Encodes a particular value and writes its encoded
        /// representation to a buffer.
        /// </summary>
        /// <param name="value">A value to encode.</param>
        /// <param name="buffer">
        /// A buffer to write <paramref name="value"/>'s encoded representation to.
        /// </param>
        public void Encode(TObj value, TPtr buffer)
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
                EncodePointer(buffer, address);
            }
            else
            {
                EncodeStruct(value, buffer);
            }
        }

        private void EncodeStruct(TObj value, TPtr buffer)
        {
            foreach (var pair in GetFieldValues(value))
            {
                Encode(pair.Value, IndexPointer(buffer, GetFieldOffset(pair.Key)));
            }
        }

        protected TPtr EncodeBoxPointer(TObj box, IType elementType)
        {
            TPtr address;
            if (!encoded.TryGetValue(box, out address))
            {
                encoded[box] = address = CreateObject(elementType);
                Encode(LoadBoxPointer(box), address);
            }
            return address;
        }
    }
}
