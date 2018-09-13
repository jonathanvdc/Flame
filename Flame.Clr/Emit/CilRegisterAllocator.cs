using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Flame.Clr.Emit
{
    /// <summary>
    /// A register allocator for the CIL backend.
    /// </summary>
    public sealed class CilRegisterAllocator : GreedyRegisterAllocator<CilCodegenRegister>
    {
        /// <summary>
        /// Creates a CIL register allocator that will only allocate
        /// registers to a particular set of values.
        /// </summary>
        /// <param name="usedValues">
        /// The values to allocate registers to.
        /// </param>
        /// <param name="paramRegisters">
        /// The registers assigned to the method's parameters.
        /// </param>
        public CilRegisterAllocator(
            HashSet<ValueTag> usedValues,
            Dictionary<ValueTag, ParameterDefinition> paramRegisters)
        {
            this.usedValues = usedValues;
            this.paramRegisters = paramRegisters;
        }

        private HashSet<ValueTag> usedValues;
        private Dictionary<ValueTag, ParameterDefinition> paramRegisters;

        /// <inheritdoc/>
        protected override bool RequiresRegister(
            ValueTag value,
            FlowGraph graph)
        {
            return usedValues.Contains(value);
        }

        /// <inheritdoc/>
        protected override bool TryGetPreallocatedRegister(
            ValueTag value,
            FlowGraph graph,
            out CilCodegenRegister register)
        {
            ParameterDefinition parameter;
            if (paramRegisters.TryGetValue(value, out parameter))
            {
                register = new CilCodegenRegister(parameter);
                return true;
            }
            else
            {
                register = default(CilCodegenRegister);
                return false;
            }
        }

        /// <inheritdoc/>
        protected override CilCodegenRegister CreateRegister(IType type)
        {
            return new CilCodegenRegister(
                new VariableDefinition(TypeHelpers.ToTypeReference(type)));
        }

        /// <inheritdoc/>
        protected override bool TryRecycleRegister(
            IType type,
            IEnumerable<CilCodegenRegister> registers,
            out CilCodegenRegister result)
        {
            var typeRef = TypeHelpers.ToTypeReference(type);
            result = registers.FirstOrDefault(reg => reg.Type == typeRef);
            return result.IsParameter || result.IsVariable;
        }
    }

    /// <summary>
    /// A register as produced by the CIL register allocator.
    /// </summary>
    public struct CilCodegenRegister
    {
        /// <summary>
        /// Creates a register from a parameter definition.
        /// </summary>
        /// <param name="parameter">
        /// A parameter definition.
        /// </param>
        public CilCodegenRegister(ParameterDefinition parameter)
        {
            this.ParameterOrNull = parameter;
            this.VariableOrNull = null;
        }

        /// <summary>
        /// Creates a register from a variable definition.
        /// </summary>
        /// <param name="variable">
        /// A variable definition.
        /// </param>
        public CilCodegenRegister(VariableDefinition variable)
        {
            this.ParameterOrNull = null;
            this.VariableOrNull = variable;
        }

        public ParameterDefinition ParameterOrNull { get; private set; }
        public VariableDefinition VariableOrNull { get; private set; }

        public bool IsParameter => ParameterOrNull != null;
        public bool IsVariable => VariableOrNull != null;

        public TypeReference Type => IsParameter
            ? ParameterOrNull.ParameterType
            : VariableOrNull.VariableType;
    }
}
