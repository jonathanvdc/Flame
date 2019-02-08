using System.Collections.Generic;
using System.Linq;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// An instruction prototype for intrinsics: instructions that behave
    /// like calls but are not (necessarily) implemented as calls.
    ///
    /// Various parts of a compiler recognize intrinsics relevant to them
    /// and ignore the others.
    /// </summary>
    public sealed class IntrinsicPrototype : InstructionPrototype
    {
        private IntrinsicPrototype(
            string name,
            IType returnType,
            IReadOnlyList<IType> parameterTypes)
        {
            this.Name = name;
            this.returnType = returnType;
            this.ParameterTypes = parameterTypes;
        }

        /// <summary>
        /// Gets this intrinsic's name.
        /// </summary>
        /// <returns>The intrinsic's name.</returns>
        public string Name { get; private set; }

        /// <summary>
        /// Gets this intrinsic's parameter types.
        /// </summary>
        /// <returns>The parameter types.</returns>
        public IReadOnlyList<IType> ParameterTypes { get; private set; }

        private IType returnType;

        /// <inheritdoc/>
        public override IType ResultType => returnType;

        /// <inheritdoc/>
        public override int ParameterCount => ParameterTypes.Count;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(Instruction instance, MethodBody body)
        {
            var errors = new List<string>();

            var argList = GetArgumentList(instance);
            int paramCount = ParameterTypes.Count;
            for (int i = 0; i < paramCount; i++)
            {
                var paramType = ParameterTypes[i];
                var argType = body.Implementation.GetValueType(argList[i]);

                if (!paramType.Equals(argType))
                {
                    errors.Add(
                        string.Format(
                            "Argument of type '{0}' was provided where an " +
                            "argument of type '{1}' was expected.",
                            paramType.FullName,
                            argType.FullName));
                }
            }

            return errors;
        }

        /// <inheritdoc/>
        public override InstructionPrototype Map(MemberMapping mapping)
        {
            return Create(
                Name,
                mapping.MapType(ResultType),
                ParameterTypes.EagerSelect<IType, IType>(mapping.MapType));
        }

        /// <summary>
        /// Gets the argument list in an instruction that conforms to
        /// this prototype.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that conforms to this prototype.
        /// </param>
        /// <returns>The formal argument list.</returns>
        public IReadOnlyList<ValueTag> GetArgumentList(Instruction instruction)
        {
            AssertIsPrototypeOf(instruction);
            return instruction.Arguments;
        }

        /// <summary>
        /// Instantiates this prototype with a list of arguments.
        /// </summary>
        /// <param name="arguments">
        /// The arguments to instantiate this prototype with.
        /// </param>
        /// <returns>
        /// An instruction whose prototype is equal to this prototype
        /// and whose argument list is <paramref name="arguments"/>.
        /// </returns>
        public Instruction Instantiate(params ValueTag[] arguments)
        {
            return Instantiate((IReadOnlyList<ValueTag>)arguments);
        }

        private static readonly InterningCache<IntrinsicPrototype> instanceCache
            = new InterningCache<IntrinsicPrototype>(
                new StructuralIntrinsicPrototypeComparer());

        /// <summary>
        /// Gets the intrinsic instruction prototype for a particular intrinsic name,
        /// return type and parameter type list.
        /// </summary>
        /// <param name="name">The intrinsic's name.</param>
        /// <param name="returnType">The type of value returned by the intrinsic.</param>
        /// <param name="parameterTypes">A list of the intrinsic's parameter types.</param>
        /// <returns>An intrinsic instruction prototype.</returns>
        public static IntrinsicPrototype Create(
            string name,
            IType returnType,
            IReadOnlyList<IType> parameterTypes)
        {
            return instanceCache.Intern(
                new IntrinsicPrototype(name, returnType, parameterTypes));
        }
    }

    internal sealed class StructuralIntrinsicPrototypeComparer
        : IEqualityComparer<IntrinsicPrototype>
    {
        public bool Equals(IntrinsicPrototype x, IntrinsicPrototype y)
        {
            return x.Name == y.Name
                && object.Equals(x.ResultType, y.ResultType)
                && x.ParameterTypes.SequenceEqual<IType>(y.ParameterTypes);
        }

        public int GetHashCode(IntrinsicPrototype obj)
        {
            int hashCode = EnumerableComparer.EmptyHash;
            hashCode = EnumerableComparer.FoldIntoHashCode(hashCode, obj.ResultType);
            var paramTypes = obj.ParameterTypes;
            var paramTypeCount = paramTypes.Count;
            for (int i = 0; i < paramTypeCount; i++)
            {
                hashCode = EnumerableComparer.FoldIntoHashCode(hashCode, paramTypes[i]);
            }
            hashCode = EnumerableComparer.FoldIntoHashCode(hashCode, obj.Name);
            return hashCode;
        }
    }
}
