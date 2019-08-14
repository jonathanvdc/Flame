using System;
using System.Collections.Generic;
using Flame.Llvm.Emit;
using Flame.TypeSystem;
using LLVMSharp;

namespace Flame.Llvm
{
    /// <summary>
    /// Decodes in-memory representations of objects.
    /// </summary>
    /// <typeparam name="TObj">The type of a decoded object.</typeparam>
    /// <typeparam name="TPtr">The type of a pointer to an encoded object.</typeparam>
    public abstract class ObjectDecoder<TObj, TPtr> : ObjectMarshal<TObj>
    {
        /// <summary>
        /// Creates an object decoder.
        /// </summary>
        /// <param name="compiledModule">
        /// A compiled LLVM module to inspect for type data layouts.
        /// </param>
        /// <param name="target">
        /// A target data layout to use for determining the precise layout of data types.
        /// </param>
        /// <param name="target">
        /// A mapping of pointers to existing objects. When a key in this dictionary
        /// is encountered during decoding, the corresponding value is updated.
        /// Pointers that are not keys in this dictionary are mapped to objects that are
        /// newly created during decoding.
        /// </param>
        public ObjectDecoder(
            ModuleBuilder compiledModule,
            LLVMTargetDataRef target,
            IReadOnlyDictionary<TPtr, TObj> existingObjects)
            : base(compiledModule, target)
        {
            this.ExistingObjects = existingObjects;
            this.decoded = new Dictionary<TPtr, TObj>();
        }

        /// <summary>
        /// Gets a mapping of pointers to existing objects. When a key in this dictionary
        /// is encountered during decoding, the corresponding value is updated.
        /// Pointers that are not keys in this dictionary are mapped to objects that are
        /// newly created during decoding.
        /// </summary>
        /// <value>A mapping of pointers to objects.</value>
        public IReadOnlyDictionary<TPtr, TObj> ExistingObjects { get; private set; }

        private Dictionary<TPtr, TObj> decoded;

        /// <summary>
        /// Adds an offset to a pointer.
        /// </summary>
        /// <param name="pointer">A base pointer.</param>
        /// <param name="offset">An offset to add to <paramref name="pointer"/>.</param>
        /// <returns>A modified pointer.</returns>
        public abstract TPtr IndexPointer(TPtr pointer, int offset);

        /// <summary>
        /// Gets the type of an in-memory object stored at a particular address.
        /// </summary>
        /// <param name="pointer">A pointer to an in-memory object.</param>
        /// <returns>The type of the object at address <paramref name="pointer"/>.</returns>
        public abstract IType TypeOf(TPtr pointer);

        /// <summary>
        /// Dereferences a pointer to a boxed object reference.
        /// </summary>
        /// <param name="pointer">The pointer to dereference.</param>
        /// <returns>A boxed object reference.</returns>
        public abstract TPtr LoadBoxPointer(TPtr pointer);

        /// <summary>
        /// Creates a new object of a particular type.
        /// </summary>
        /// <param name="type">The type of object to create.</param>
        /// <returns>An instance of <paramref name="type"/>.</returns>
        public abstract TObj CreateObject(IType type);

        /// <summary>
        /// Decodes a primitive object stored at a particular address, provided that
        /// the object is indeed a primitive object.
        /// </summary>
        /// <param name="pointer">The address of the object to decode.</param>
        /// <param name="type">The type of the object to decode.</param>
        /// <param name="obj">A decoded primitive object.</param>
        /// <returns>
        /// <c>true</c> if the object stored at <paramref name="pointer"/> is a primitive object
        /// and has been decoded; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool TryDecodePrimitive(TPtr pointer, IType type, out TObj obj);

        /// <summary>
        /// Updates a primitive object's data with data stored at a particular address,
        /// provided that the object is indeed a primitive object.
        /// </summary>
        /// <param name="pointer">The address of <paramref name="obj"/>'s new data.</param>
        /// <param name="type">The type of <paramref name="obj"/>.</param>
        /// <param name="obj">The primitive object to update.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is a primitive object
        /// and has been updated; otherwise, <c>false</c>.
        /// </returns>
        public abstract bool TryUpdatePrimitive(TPtr pointer, TObj obj, IType type);

        /// <summary>
        /// Updates the value of an object's field.
        /// </summary>
        /// <param name="obj">An object that contains <paramref name="field"/>.</param>
        /// <param name="field">A field to update.</param>
        /// <param name="value">The value to assign to <paramref name="field"/> in <paramref name="obj"/>.</param>
        public abstract void SetField(TObj obj, IField field, TObj value);

        /// <summary>
        /// Registers an object as the decoded or updated version of the data
        /// at a particular address.
        /// </summary>
        /// <param name="pointer">A pointer to a decoded object.</param>
        /// <param name="obj">
        /// An object that corresponds to the decoded or updated
        /// version of <paramref name="pointer"/>.
        /// </param>
        protected void RegisterDecoded(TPtr pointer, TObj obj)
        {
            decoded[pointer] = obj;
        }

        private void DecodeFields(TPtr pointer, TObj obj, IType type)
        {
            foreach (var field in type.Fields)
            {
                if (field.IsStatic)
                {
                    continue;
                }

                SetField(obj, field, DecodeField(pointer, field));
            }
        }

        private TObj DecodeField(TPtr pointer, IField field)
        {
            var fieldType = field.FieldType;
            if (fieldType.IsPointerType(PointerKind.Box))
            {
                return Decode(LoadBoxPointer(IndexPointer(pointer, GetFieldOffset(field))));
            }
            else
            {
                return DecodeData(pointer, fieldType);
            }
        }

        private TObj DecodeData(TPtr pointer, IType type)
        {
            TObj obj;
            if (!ExistingObjects.TryGetValue(pointer, out obj))
            {
                if (TryDecodePrimitive(pointer, type, out obj))
                {
                    RegisterDecoded(pointer, obj);
                    return obj;
                }
                else
                {
                    obj = CreateObject(type);
                }
            }
            RegisterDecoded(pointer, obj);
            if (!TryUpdatePrimitive(pointer, obj, type))
            {
                DecodeFields(pointer, obj, type);
            }
            return obj;
        }

        /// <summary>
        /// Decodes an object stored at a particular address.
        /// </summary>
        /// <param name="pointer">A pointer to an object to decode.</param>
        /// <returns>A decoded version of the object stored at <paramref name="pointer"/>.</returns>
        public TObj Decode(TPtr pointer)
        {
            TObj obj;
            if (!decoded.TryGetValue(pointer, out obj))
            {
                obj = DecodeData(pointer, TypeOf(pointer));
            }
            return obj;
        }
    }
}
