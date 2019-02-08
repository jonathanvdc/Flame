using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// An instruction prototype for call instructions: instructions that
    /// call a method.
    /// </summary>
    public sealed class CallPrototype : InstructionPrototype
    {
        private CallPrototype(IMethod callee, MethodLookup lookup)
        {
            this.Callee = callee;
            this.Lookup = lookup;
        }

        /// <summary>
        /// Gets the method to call.
        /// </summary>
        /// <returns>The callee.</returns>
        public IMethod Callee { get; private set; }

        /// <summary>
        /// Gets the method lookup strategy used by this call.
        /// </summary>
        /// <returns></returns>
        public MethodLookup Lookup { get; private set; }

        /// <inheritdoc/>
        public override IType ResultType
            => Callee.ReturnParameter.Type;

        /// <inheritdoc/>
        public override int ParameterCount
            => (Callee.IsStatic ? 0 : 1) + Callee.Parameters.Count;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(
            Instruction instance,
            MethodBody body)
        {
            var errors = new List<string>();

            bool isStatic = Callee.IsStatic;
            if (!isStatic)
            {
                var thisType = body.Implementation
                    .GetValueType(GetThisArgument(instance))
                    as PointerType;

                if (thisType == null)
                {
                    errors.Add("Type of the 'this' argument must be a pointer type.");
                }
                else if (!thisType.ElementType.Equals(Callee.ParentType))
                {
                    errors.Add(
                        string.Format(
                            "Pointee type of 'this' argument type '{0}' should " +
                            "have been parent type '{1}'.",
                            thisType.FullName,
                            Callee.ParentType.FullName));
                }
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

            errors.AddRange(CheckArgumentTypes(argList, Callee.Parameters, body));

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
                return Create(newMethod, Lookup);
            }
        }

        /// <summary>
        /// Instantiates this call instruction prototype.
        /// </summary>
        /// <param name="thisArgument">
        /// The 'this' argument for the call.
        /// </param>
        /// <param name="arguments">
        /// The argument list for the call.
        /// </param>
        /// <returns>
        /// A call instruction.
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

        internal static IReadOnlyList<string> CheckArgumentTypes(
            ReadOnlySlice<ValueTag> arguments,
            IReadOnlyList<Parameter> parameters,
            MethodBody body)
        {
            var errors = new List<string>();

            int paramCount = parameters.Count;
            for (int i = 0; i < paramCount; i++)
            {
                var paramType = parameters[i].Type;
                var argType = body.Implementation.GetValueType(arguments[i]);

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
            ContractHelpers.Assert(!Callee.IsStatic);
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
            int offset = Callee.IsStatic ? 0 : 1;
            return new ReadOnlySlice<ValueTag>(
                instruction.Arguments,
                offset,
                instruction.Arguments.Count - offset);
        }

        private static readonly InterningCache<CallPrototype> instanceCache
            = new InterningCache<CallPrototype>(
                new StructuralCallPrototypeComparer());

        /// <summary>
        /// Gets the call instruction prototype for a particular callee.
        /// </summary>
        /// <param name="callee">The method to call.</param>
        /// <param name="lookup">The method lookup strategy for the call.</param>
        /// <returns>A call instruction prototype.</returns>
        public static CallPrototype Create(IMethod callee, MethodLookup lookup)
        {
            ContractHelpers.Assert(
                lookup == MethodLookup.Static || !callee.IsStatic,
                "A static callee method cannot be resolved via virtual lookup.");
            return instanceCache.Intern(new CallPrototype(callee, lookup));
        }
    }

    internal sealed class StructuralCallPrototypeComparer
        : IEqualityComparer<CallPrototype>
    {
        public bool Equals(CallPrototype x, CallPrototype y)
        {
            return object.Equals(x.Callee, y.Callee) && x.Lookup == y.Lookup;
        }

        public int GetHashCode(CallPrototype obj)
        {
            return (obj.Callee.GetHashCode() << 1) ^ obj.Lookup.GetHashCode();
        }
    }
}