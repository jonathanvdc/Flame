using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// A prototype for instructions that box value types.
    /// </summary>
    public sealed class BoxPrototype : InstructionPrototype
    {
        private BoxPrototype(PointerType boxType)
        {
            this.boxType = boxType;
        }

        private PointerType boxType;

        /// <summary>
        /// Gets the type of value that is boxed by instances of
        /// this prototype.
        /// </summary>
        public IType ElementType => boxType.ElementType;

        /// <inheritdoc/>
        public override IType ResultType => boxType;

        /// <inheritdoc/>
        public override int ParameterCount => 1;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(Instruction instance, MethodBody body)
        {
            var valType = body.Implementation.GetValueType(GetBoxedValue(instance));
            if (!valType.Equals(ElementType))
            {
                return new[]
                {
                    "Box instruction that takes type '" + ElementType +
                    "' cannot be passed a value of type '" + valType + "'."
                };
            }
            else
            {
                return EmptyArray<string>.Value;
            }
        }

        /// <inheritdoc/>
        public override InstructionPrototype Map(MemberMapping mapping)
        {
            return Create(mapping.MapType(ElementType));
        }

        /// <summary>
        /// Gets the value boxed by an instance of this prototype.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that conforms to this prototype.
        /// </param>
        /// <returns>The boxed value.</returns>
        public ValueTag GetBoxedValue(Instruction instruction)
        {
            AssertIsPrototypeOf(instruction);
            return instruction.Arguments[0];
        }

        /// <summary>
        /// Instantiates this box instruction prototype.
        /// </summary>
        /// <param name="value">
        /// The value to box.
        /// </param>
        /// <returns>
        /// A box instruction.
        /// </returns>
        public Instruction Instantiate(ValueTag value)
        {
            return Instantiate(new ValueTag[] { value });
        }

        private static readonly InterningCache<BoxPrototype> instanceCache
            = new InterningCache<BoxPrototype>(
                new StructuralBoxPrototypeComparer());

        /// <summary>
        /// Gets the box instruction prototype for a particular result type.
        /// </summary>
        /// <param name="elementType">The type of value to box.</param>
        /// <returns>A box instruction prototype.</returns>
        public static BoxPrototype Create(IType elementType)
        {
            return instanceCache.Intern(new BoxPrototype(elementType.MakePointerType(PointerKind.Box)));
        }
    }

    internal sealed class StructuralBoxPrototypeComparer
        : IEqualityComparer<BoxPrototype>
    {
        public bool Equals(BoxPrototype x, BoxPrototype y)
        {
            return object.Equals(x.ResultType, y.ResultType);
        }

        public int GetHashCode(BoxPrototype obj)
        {
            return obj.ResultType.GetHashCode();
        }
    }
}
