using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// An instruction prototype for constrained call instructions:
    /// instructions that call a method using virtual lookup in a way
    /// that is suitable for both reference and value types.
    /// </summary>
    public sealed class ConstrainedCallPrototype : InstructionPrototype
    {
        private ConstrainedCallPrototype(IMethod callee)
        {
            this.Callee = callee;
        }

        /// <summary>
        /// Gets the method to call.
        /// </summary>
        /// <returns>The callee.</returns>
        public IMethod Callee { get; private set; }

        /// <inheritdoc/>
        public override IType ResultType
            => Callee.ReturnParameter.Type;

        /// <inheritdoc/>
        public override int ParameterCount
            => 1 + Callee.Parameters.Count;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(
            Instruction instance,
            MethodBody body)
        {
            var errors = new List<string>();

            var thisPtrType = body.Implementation
                .GetValueType(GetThisArgument(instance))
                as PointerType;

            if (thisPtrType == null || thisPtrType.Kind != PointerKind.Reference)
            {
                errors.Add("Type of the 'this' argument must be a reference pointer type.");
            }

            var parameters = Callee.Parameters;
            var argList = GetArgumentList(instance);
            int paramCount = parameters.Count;
            for (int i = 0; i < paramCount; i++)
            {
                var paramType = parameters[i].Type;
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

            errors.AddRange(
                CallPrototype.CheckArgumentTypes(argList, Callee.Parameters, body));

            return errors;
        }

        /// <inheritdoc/>
        public override InstructionPrototype Map(MemberMapping mapping)
        {
            var newMethod = mapping.MapMethod(Callee);
            if (object.ReferenceEquals(newMethod, Callee))
            {
                return this;
            }
            else
            {
                return Create(newMethod);
            }
        }

        /// <summary>
        /// Instantiates this constrained call instruction prototype.
        /// </summary>
        /// <param name="thisArgument">
        /// The 'this' argument for the call.
        /// </param>
        /// <param name="arguments">
        /// The argument list for the call.
        /// </param>
        /// <returns>
        /// A constrained call instruction.
        /// </returns>
        public Instruction Instantiate(
            ValueTag thisArgument,
            IReadOnlyList<ValueTag> arguments)
        {
            var extendedArgs = new List<ValueTag>();
            extendedArgs.Add(thisArgument);
            extendedArgs.AddRange(arguments);
            return Instantiate(extendedArgs);
        }

        /// <summary>
        /// Gets the 'this' argument in an instruction that conforms to
        /// this prototype.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that conforms to this prototype.
        /// </param>
        /// <returns>The 'this' argument.</returns>
        public ValueTag GetThisArgument(Instruction instruction)
        {
            AssertIsPrototypeOf(instruction);
            return instruction.Arguments[0];
        }

        /// <summary>
        /// Gets the argument list in an instruction that conforms to
        /// this prototype.
        /// </summary>
        /// <param name="instruction">
        /// An instruction that conforms to this prototype.
        /// </param>
        /// <returns>The formal argument list.</returns>
        public ReadOnlySlice<ValueTag> GetArgumentList(Instruction instruction)
        {
            AssertIsPrototypeOf(instruction);
            int offset = 1;
            return new ReadOnlySlice<ValueTag>(
                instruction.Arguments,
                offset,
                instruction.Arguments.Count - offset);
        }

        private static readonly InterningCache<ConstrainedCallPrototype> instanceCache
            = new InterningCache<ConstrainedCallPrototype>(
                new MappedComparer<ConstrainedCallPrototype, IMethod>(proto => proto.Callee));

        /// <summary>
        /// Gets the constrained call instruction prototype for a particular callee.
        /// </summary>
        /// <param name="callee">The method to call.</param>
        /// <returns>A constrained call instruction prototype.</returns>
        public static ConstrainedCallPrototype Create(IMethod callee)
        {
            ContractHelpers.Assert(
                !callee.IsStatic,
                "Constrained calls cannot call static methods.");
            return instanceCache.Intern(new ConstrainedCallPrototype(callee));
        }
    }
}
