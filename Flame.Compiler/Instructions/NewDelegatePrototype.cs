using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Instructions
{
    /// <summary>
    /// A prototype for an instruction that creates a delegate from
    /// a method.
    /// </summary>
    public sealed class NewDelegatePrototype : InstructionPrototype
    {
        private NewDelegatePrototype(
            IType delegateType,
            IMethod callee,
            bool hasThisParameter,
            MethodLookup lookup)
        {
            this.delegateType = delegateType;
            this.Callee = callee;
            this.HasThisArgument = hasThisParameter;
            this.Lookup = lookup;
        }

        private IType delegateType;

        /// <summary>
        /// Gets the method that is called by the delegate when invoked.
        /// </summary>
        /// <returns>The callee method.</returns>
        public IMethod Callee { get; private set; }

        /// <summary>
        /// Tells if this new-delegate instruction prototype takes a 'this'
        /// argument. For instance methods, the 'this' argument is obviously
        /// interpreted as the 'this' pointer. For static methods, the 'this'
        /// argument is sent to the first argument in the parameter list.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this instruction prototype takes a 'this'
        /// argument; otherwise, <c>false</c>.
        /// </returns>
        public bool HasThisArgument { get; private set; }

        /// <summary>
        /// Gets the method lookup strategy for this new-delegate instruction
        /// prototype.
        /// </summary>
        /// <returns>The method lookup strategy.</returns>
        public MethodLookup Lookup { get; private set; }

        /// <inheritdoc/>
        public override IType ResultType => delegateType;

        /// <inheritdoc/>
        public override int ParameterCount => HasThisArgument ? 1 : 0;

        /// <inheritdoc/>
        public override ExceptionSpecification ExceptionSpecification
            // TODO: maybe refine the exception specification to `throws (NullPointerException)`
            // for the `HasThisArgument` case somehow?
            => HasThisArgument
                ? ExceptionSpecification.ThrowAny
                : ExceptionSpecification.NoThrow;

        /// <inheritdoc/>
        public override IReadOnlyList<string> CheckConformance(Instruction instance, MethodBody body)
        {
            if (HasThisArgument)
            {
                var firstArgType = body.Implementation
                    .GetValueType(GetThisArgument(instance));

                if (!Callee.IsStatic)
                {
                    var thisType = firstArgType as PointerType;

                    if (thisType == null)
                    {
                        return new string[]
                        {
                            "Type of the 'this' argument must be a pointer type."
                        };
                    }
                    else if (!thisType.ElementType.Equals(Callee.ParentType))
                    {
                        return new string[]
                        {
                            string.Format(
                                "Pointee type of 'this' argument type '{0}' should " +
                                "have been parent type '{1}'.",
                                thisType.FullName,
                                Callee.ParentType.FullName)
                        };
                    }
                }
                else
                {
                    var paramType = Callee.Parameters[0].Type;

                    if (!paramType.Equals(firstArgType))
                    {
                        return new string[]
                        {
                            string.Format(
                                "First (pseudo-this) argument of type '{0}' was " +
                                "provided where an argument of type '{1}' was expected.",
                                paramType.FullName,
                                firstArgType.FullName)
                        };
                    }
                }
            }
            return EmptyArray<string>.Value;
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
            ContractHelpers.Assert(HasThisArgument);
            return instruction.Arguments[0];
        }

        /// <summary>
        /// Creates an instance of this new-delegate instruction prototype.
        /// </summary>
        /// <param name="thisArgument">
        /// The 'this' argument, if any. A <c>null</c> value means that
        /// there is no 'this' argument.
        /// </param>
        /// <returns>A new-delegate instruction.</returns>
        public Instruction Instantiate(ValueTag thisArgument)
        {
            return Instantiate(
                thisArgument == null
                ? EmptyArray<ValueTag>.Value
                : new ValueTag[] { thisArgument });
        }

        private static readonly InterningCache<NewDelegatePrototype> instanceCache
            = new InterningCache<NewDelegatePrototype>(
                new StructuralNewDelegatePrototypeComparer());

        /// <summary>
        /// Gets or creates a new-delegate instruction prototype.
        /// </summary>
        /// <param name="delegateType">
        /// The type of delegate produced by instances of the prototype.
        /// </param>
        /// <param name="callee">
        /// The method that is invoked when the delegates produced by instances
        /// of the prototype are called.
        /// </param>
        /// <param name="hasThisParameter">
        /// Tells if a 'this' parameter is included in the delegate.
        /// </param>
        /// <param name="lookup">
        /// The method lookup strategy for the prototype.
        /// </param>
        /// <returns>A new-delegate instruction prototype.</returns>
        public static NewDelegatePrototype Create(
            IType delegateType,
            IMethod callee,
            bool hasThisParameter,
            MethodLookup lookup)
        {
            if (hasThisParameter)
            {
                ContractHelpers.Assert(
                    !callee.IsStatic || callee.Parameters.Count >= 1,
                    "A callee that is provided a 'this' argument must " +
                    "be an instance method or take at least one parameter.");
            }
            ContractHelpers.Assert(
                lookup == MethodLookup.Static || !callee.IsStatic,
                "A static callee method cannot be resolved via virtual lookup.");

            return instanceCache.Intern(
                new NewDelegatePrototype(
                    delegateType,
                    callee,
                    hasThisParameter,
                    lookup));
        }
    }

    internal sealed class StructuralNewDelegatePrototypeComparer
        : IEqualityComparer<NewDelegatePrototype>
    {
        public bool Equals(NewDelegatePrototype x, NewDelegatePrototype y)
        {
            return object.Equals(x.ResultType, y.ResultType)
                && object.Equals(x.Callee, y.Callee)
                && x.HasThisArgument == y.HasThisArgument
                && x.Lookup == y.Lookup;
        }

        public int GetHashCode(NewDelegatePrototype obj)
        {
            int result = obj.ResultType.GetHashCode();
            result = (result << 3) ^ obj.Callee.GetHashCode();
            result = (result << 1) ^ obj.HasThisArgument.GetHashCode();
            result = (result << 1) ^ obj.Lookup.GetHashCode();
            return result;
        }
    }
}