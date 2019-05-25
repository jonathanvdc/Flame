using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// A prototype for instructions that compute the address of
    /// a field from a base address.
    /// </summary>
    public sealed class GetStaticFieldPointerPrototype : InstructionPrototype
    {
        private GetStaticFieldPointerPrototype(IField field)
        {
            this.Field = field;
            this.fieldPointerType = field.FieldType.MakePointerType(PointerKind.Reference);
        }

        /// <summary>
        /// Gets the field whose address is taken.
        /// </summary>
        /// <value>The field whose address is taken.</value>
        public IField Field { get; private set; }

        private IType fieldPointerType;

        /// <inheritdoc/>
        public override IType ResultType => fieldPointerType;

        /// <inheritdoc/>
        public override int ParameterCount => 0;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(Instruction instance, MethodBody body)
        {
            return EmptyArray<string>.Value;
        }

        /// <inheritdoc/>
        public override InstructionPrototype Map(MemberMapping mapping)
        {
            return Create(mapping.MapField(Field));
        }

        /// <summary>
        /// Creates an instance of this get-static-field-pointer prototype.
        /// </summary>
        /// <returns>A get-static-field-pointer instruction.</returns>
        public Instruction Instantiate()
        {
            return Instantiate(EmptyArray<ValueTag>.Value);
        }

        private static readonly InterningCache<GetStaticFieldPointerPrototype> instanceCache
            = new InterningCache<GetStaticFieldPointerPrototype>(
                new StructuralGetStaticFieldPointerPrototypeComparer());

        /// <summary>
        /// Gets or creates a get-static-field-pointer instruction prototype
        /// that computes the address of a particular field.
        /// </summary>
        /// <param name="field">
        /// The field whose address is to be computed.
        /// </param>
        /// <returns>
        /// A get-static-field-pointer instruction prototype.
        /// </returns>
        public static GetStaticFieldPointerPrototype Create(IField field)
        {
            return instanceCache.Intern(new GetStaticFieldPointerPrototype(field));
        }
    }

    internal sealed class StructuralGetStaticFieldPointerPrototypeComparer
        : IEqualityComparer<GetStaticFieldPointerPrototype>
    {
        public bool Equals(GetStaticFieldPointerPrototype x, GetStaticFieldPointerPrototype y)
        {
            return x.Field == y.Field;
        }

        public int GetHashCode(GetStaticFieldPointerPrototype obj)
        {
            return obj.Field.GetHashCode();
        }
    }
}
