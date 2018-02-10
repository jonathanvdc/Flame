using Flame.Compiler.Instructions;

namespace Flame.Compiler
{
    public partial struct Instruction
    {
        /// <summary>
        /// Creates an instruction that allocates storage on the stack
        /// for a variable number of elements of a particular type.
        /// </summary>
        /// <param name="elementType">
        /// The type of value to allocate storage for.
        /// </param>
        /// <returns>
        /// An alloca-array instruction.
        /// </returns>
        public static Instruction CreateAllocaArray(
            IType elementType, ValueTag elementCount)
        {
            return AllocaArrayPrototype.Create(elementType).Instantiate(elementCount);
        }

        /// <summary>
        /// Creates an instruction that allocates storage on the stack
        /// for a single value element of a particular type.
        /// </summary>
        /// <param name="elementType">
        /// The type of value to allocate storage for.
        /// </param>
        /// <returns>
        /// An alloca instruction.
        /// </returns>
        public static Instruction CreateAlloca(IType elementType)
        {
            return AllocaPrototype.Create(elementType).Instantiate();
        }

        /// <summary>
        /// Creates an instruction that creates a constant value of a
        /// particular type.
        /// </summary>
        /// <param name="value">
        /// The constant value to produce.
        /// </param>
        /// <param name="type">
        /// The type of value created by the instruction.
        /// </param>
        /// <returns>
        /// A constant instruction.
        /// </returns>
        public static Instruction CreateConstant(Constant value, IType type)
        {
            return ConstantPrototype.Create(value, type).Instantiate();
        }
    }
}