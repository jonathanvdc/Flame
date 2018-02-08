using System.Collections.Generic;
using System.Collections.Immutable;
using Flame.Collections;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// The prototype for copy instructions.
    /// </summary>
    public sealed class CopyPrototype : InstructionPrototype
    {
        private CopyPrototype(IType resultType)
        {
            this.type = resultType;
        }

        private IType type;

        /// <inheritdoc/>
        public override IType ResultType => type;

        /// <inheritdoc/>
        public override int ParameterCount => 1;

        /// <inheritdoc/>
        public override ExceptionSpecification ExceptionSpecification
            => ExceptionSpecification.NoThrow;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(
            Instruction instance,
            MethodBody body)
        {
            var inputType = body.Implementation.GetValueType(instance.Arguments[0]);
            if (inputType.Equals(ResultType))
            {
                return ImmutableList<string>.Empty;
            }
            else
            {
                return ImmutableList<string>.Empty.Add(
                    string.Format(
                        "Input type '{0}' does not match result type '{1}'.",
                        inputType,
                        ResultType));
            }
        }

        private static readonly InterningCache<CopyPrototype> instanceCache
            = new InterningCache<CopyPrototype>(
                new StructuralCopyPrototypeComparer());

        /// <summary>
        /// Gets the copy instruction prototype for a particular result type.
        /// </summary>
        /// <param name="resultType">The result type.</param>
        /// <returns>A copy instruction prototype.</returns>
        public static CopyPrototype Create(IType resultType)
        {
            return instanceCache.Intern(new CopyPrototype(resultType));
        }
    }

    internal sealed class StructuralCopyPrototypeComparer
        : IEqualityComparer<CopyPrototype>
    {
        public bool Equals(CopyPrototype x, CopyPrototype y)
        {
            return object.Equals(x.ResultType, y.ResultType);
        }

        public int GetHashCode(CopyPrototype obj)
        {
            return obj.ResultType.GetHashCode();
        }
    }
}