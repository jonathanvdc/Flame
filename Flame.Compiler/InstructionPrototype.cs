using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Flame.TypeSystem;

namespace Flame.Compiler
{
    /// <summary>
    /// Describes an instruction's prototype: everything there is to
    /// an instruction except for its arguments.
    /// </summary>
    public abstract class InstructionPrototype
    {
        /// <summary>
        /// Gets the type of value produced instantiations of this prototype.
        /// </summary>
        /// <returns>A type of value.</returns>
        public abstract IType ResultType { get; }

        /// <summary>
        /// Gets the number of arguments this instruction takes when instantiated.
        /// </summary>
        /// <returns>The number of arguments this instruction takes.</returns>
        public abstract int ParameterCount { get; }

        /// <summary>
        /// Checks if a particular instance of this prototype conforms to
        /// the rules for this instruction prototype.
        /// </summary>
        /// <param name="instance">
        /// An instance of this prototype.
        /// </param>
        /// <param name="body">
        /// The method body that defines the instruction.
        /// </param>
        /// <returns>
        /// A list of conformance errors in the instruction.
        /// </returns>
        public abstract IReadOnlyList<string> CheckConformance(
            Instruction instance,
            MethodBody body);

        /// <summary>
        /// Applies a member mapping to this instruction prototype.
        /// </summary>
        /// <param name="mapping">A member mapping.</param>
        /// <returns>A transformed instruction prototype.</returns>
        public abstract InstructionPrototype Map(MemberMapping mapping);

        /// <summary>
        /// Collects all members that appear in this instruction prototype.
        /// </summary>
        /// <value>A sequence of members.</value>
        public IEnumerable<IMember> Members
        {
            get
            {
                var results = new List<IMember>();
                results.Add(ResultType);
                Map(
                    new MemberMapping(
                        type =>
                        {
                            results.Add(type);
                            return type;
                        },
                        method =>
                        {
                            results.Add(method);
                            return method;
                        },
                        field =>
                        {
                            results.Add(field);
                            return field;
                        }));
                return results;
            }
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
        public Instruction Instantiate(IReadOnlyList<ValueTag> arguments)
        {
            ContractHelpers.Assert(arguments.Count == ParameterCount);
            return new Instruction(this, arguments);
        }

        /// <summary>
        /// Tests if a particular instruction is an instance of
        /// this prototype.
        /// </summary>
        /// <param name="instruction">The instruction to examine.</param>
        /// <returns>
        /// <c>true</c> if the instruction is an instance of this
        /// prototype; otherwise, <c>false</c>.
        /// </returns>
        public bool IsPrototypeOf(Instruction instruction)
        {
            return Equals(instruction.Prototype);
        }

        /// <summary>
        /// Asserts that a particular instruction is an instance of
        /// this prototype.
        /// </summary>
        /// <param name="instruction">The instruction to examine.</param>
        /// <param name="errorMessage">
        /// An error message to print if the assertion does not hold true.
        /// </param>
        public void AssertIsPrototypeOf(Instruction instruction, string errorMessage)
        {
            ContractHelpers.Assert(IsPrototypeOf(instruction), errorMessage);
        }

        /// <summary>
        /// Asserts that a particular instruction is an instance of
        /// this prototype.
        /// </summary>
        /// <param name="instruction">The instruction to examine.</param>
        public void AssertIsPrototypeOf(Instruction instruction)
        {
            AssertIsPrototypeOf(
                instruction,
                "Instruction is not an instance of prototype '" + ToString() + "'.");
        }
    }
}