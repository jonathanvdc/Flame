using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Constants;
using Flame.TypeSystem;

namespace Flame.Clr.Analysis
{
    /// <summary>
    /// The context in which CIL instructions are converted
    /// to Flame IR.
    /// </summary>
    internal sealed class CilAnalysisContext
    {
        /// <summary>
        /// Creates a CIL analysis context.
        /// </summary>
        /// <param name="block">
        /// The initial basic block to fill.
        /// </param>
        /// <param name="analyzer">
        /// The method body analyzer to which this
        /// analysis context belongs.
        /// </param>
        /// <param name="exceptionHandlers">
        /// The exception handlers for the CIL analysis context.
        /// </param>
        /// <param name="exceptionHandlerClauses">
        /// The exception handlers with clauses that include the CIL
        /// instructions being analyzed.
        /// </param>
        public CilAnalysisContext(
            BasicBlockBuilder block,
            ClrMethodBodyAnalyzer analyzer,
            IReadOnlyList<CilExceptionHandler> exceptionHandlers,
            IReadOnlyList<CilExceptionHandler> exceptionHandlerClauses)
        {
            this.Block = block;
            this.Analyzer = analyzer;
            this.ExceptionHandlers = exceptionHandlers;
            this.ExceptionHandlerClauses = exceptionHandlerClauses;
            this.stack = new Stack<ValueTag>(
                block.Parameters.Select(param => param.Tag));
            this.IsTerminated = false;
            this.isVolatileRequested = false;
        }

        /// <summary>
        /// Gets the basic block that is being created by the CIL analyzer.
        /// </summary>
        /// <value>A basic block builder.</value>
        public BasicBlockBuilder Block { get; private set; }

        /// <summary>
        /// Gets the method body analyzer to which this
        /// analysis context belongs.
        /// </summary>
        /// <value>The method body analyzer.</value>
        public ClrMethodBodyAnalyzer Analyzer { get; private set; }

        /// <summary>
        /// Gets the list of exception handlers that are responsible
        /// for handling exceptions thrown by the CIL basic block
        /// that is being analyzed.
        /// Exception handlers are tested sequentially: the first
        /// exception handler that *may* be appropriate is used to
        /// handle exceptions. It is always legal to transfer control
        /// to the first exception handler.
        /// </summary>
        /// <value>A list of exception handlers.</value>
        public IReadOnlyList<CilExceptionHandler> ExceptionHandlers { get; private set; }

        /// <summary>
        /// Gets the list of exception handlers that have clauses including
        /// the instruction currently being analyzed. The innermost handler
        /// appears first in the list.
        /// </summary>
        /// <value>A list of exception handlers.</value>
        public IReadOnlyList<CilExceptionHandler> ExceptionHandlerClauses { get; private set; }

        /// <summary>
        /// Gets the current contents of the evaluation stack.
        /// </summary>
        public IEnumerable<ValueTag> EvaluationStack => stack;

        /// <summary>
        /// Tells if the CIL basic block analyzed by this object
        /// has been terminated yet or not.
        /// </summary>
        /// <value>
        /// <c>true</c> if the basic block has been terminated yet; otherwise, <c>false</c>.
        /// </value>
        public bool IsTerminated { get; private set; }

        /// <summary>
        /// Tells if the next load or store instruction should be turned into a volatile
        /// load or store.
        /// </summary>
        private bool isVolatileRequested;

        private Stack<ValueTag> stack;

        private string GenerateInstructionName()
        {
            return Block.Tag.Name + "_val_" + Block.InstructionTags.Count;
        }

        /// <summary>
        /// Requests that the next load or store be made volatile.
        /// </summary>
        public void RequestVolatile()
        {
            isVolatileRequested = true;
        }

        /// <summary>
        /// Appends an instruction to the basic block.
        /// </summary>
        /// <param name="instruction">An instruction.</param>
        /// <returns>The value computed by <paramref name="instruction"/>.</returns>
        public ValueTag Emit(Instruction instruction)
        {
            if (isVolatileRequested)
            {
                // Make the instruction volatile if that's what the CIL stream
                // requested.
                if (instruction.Prototype is LoadPrototype)
                {
                    instruction = Instruction.CreateVolatileLoadIntrinsic(
                        (PointerType)Block.Graph.GetValueType(instruction.Arguments[0]),
                        instruction.Arguments[0]);
                    isVolatileRequested = false;
                }
                else if (instruction.Prototype is StorePrototype)
                {
                    instruction = Instruction.CreateVolatileStoreIntrinsic(
                        (PointerType)Block.Graph.GetValueType(instruction.Arguments[0]),
                        instruction.Arguments[0],
                        instruction.Arguments[1]);
                    isVolatileRequested = false;
                }
            }

            var name = GenerateInstructionName();

            // Our first step is to figure out if there's an exception handler
            // that might catch the instruction's exception.
            var exceptionSpec = Block.Graph
                .GetAnalysisResult<PrototypeExceptionSpecs>()
                .GetExceptionSpecification(instruction.Prototype);

            if (exceptionSpec.CanThrowSomething)
            {
                foreach (var handler in ExceptionHandlers)
                {
                    if (handler.IsCatchAll
                        || handler.HandledExceptionTypes.Any(exceptionSpec.CanThrow))
                    {
                        // We found an appropriate exception handler. Now all
                        // we have to do is split up the basic block and reconnect
                        // the two pieces with 'try' flow.
                        var nextBasicBlock = Block.Graph.AddBasicBlock();
                        var successTag = new ValueTag(name);
                        nextBasicBlock.AppendParameter(new BlockParameter(instruction.ResultType, successTag));

                        Block.Flow = new TryFlow(
                            instruction,
                            new Branch(nextBasicBlock, new[] { BranchArgument.TryResult }),
                            new Branch(handler.LandingPad, new[] { BranchArgument.TryException }));

                        Block = nextBasicBlock;
                        return successTag;
                    }
                }
            }

            return Block.AppendInstruction(instruction, name);
        }

        /// <summary>
        /// Pushes a reference to a value onto the evaluation stack.
        /// Values of type <c>void</c> are discarded and other values
        /// are converted to conform to the type system of the CIL stack.
        /// </summary>
        /// <param name="value">The value to push.</param>
        /// <returns>
        /// The value pushed onto the stack or the <paramref name="value"/> parameter
        /// if no value is pushed onto the stack.
        /// </returns>
        public ValueTag Push(ValueTag value)
        {
            var type = GetValueType(value);
            var typeEnv = Analyzer.TypeEnvironment;
            if (type == typeEnv.Void)
            {
                return value;
            }

            var iSpec = type.GetIntegerSpecOrNull();
            if (iSpec != null && iSpec.Size < 32)
            {
                // Convert integers smaller than 32 bits to Int32.
                value = Emit(
                    Instruction.CreateConvertIntrinsic(
                        typeEnv.Int32, type, value));
            }
            stack.Push(value);
            return value;
        }

        /// <summary>
        /// Appends an instruction to the basic block and
        /// pushes its result onto the stack.
        /// </summary>
        /// <param name="instruction">An instruction.</param>
        /// <returns>The value computed by <paramref name="instruction"/>.</returns>
        public ValueTag Push(Instruction instruction)
        {
            return Push(Emit(instruction));
        }

        /// <summary>
        /// Retrieves the value at the top of the evaluation stack.
        /// </summary>
        /// <returns>
        /// The value at the top of the evaluation stack.
        /// </returns>
        public ValueTag Peek()
        {
            return stack.Peek();
        }

        /// <summary>
        /// Pops a value from the evaluation stack.
        /// </summary>
        public ValueTag Pop()
        {
            return stack.Pop();
        }

        /// <summary>
        /// Pops a value from the evaluation stack and coerces it
        /// to a particular type.
        /// </summary>
        /// <param name="type">The type to coerce the top-of-stack value to.</param>
        public ValueTag Pop(IType type)
        {
            if (type == Analyzer.Assembly.Resolver.TypeEnvironment.Void)
            {
                return Emit(
                    Instruction.CreateDefaultConstant(type));
            }
            else
            {
                return Coerce(Pop(), type);
            }
        }

        /// <summary>
        /// Coerces a value as a particular type.
        /// </summary>
        /// <param name="value">The value to coerce.</param>
        /// <param name="type">The result type.</param>
        /// <returns>A coerced value.</returns>
        public ValueTag Coerce(ValueTag value, IType type)
        {
            var valueType = GetValueType(value);
            if (valueType.Equals(type))
            {
                // No need to emit a conversion.
                return value;
            }
            else if (valueType is PointerType && type is PointerType)
            {
                // Emit a reinterpret cast to convert between pointers.
                return Emit(
                    Instruction.CreateReinterpretCast((PointerType)type, value));
            }
            else
            {
                // Emit an 'arith.convert' intrinsic to convert between
                // primitive types.
                return Emit(
                    Instruction.CreateConvertIntrinsic(
                        type,
                        GetValueType(value),
                        value));
            }
        }

        /// <summary>
        /// Gets the type of a particular Flame IR value.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>A type.</returns>
        public IType GetValueType(ValueTag value)
        {
            return Block.Graph.GetValueType(value);
        }

        /// <summary>
        /// Terminates the block with a particular flow.
        /// </summary>
        /// <param name="flow">The flow to terminate the block with.</param>
        public void Terminate(BlockFlow flow)
        {
            Block.Flow = flow;
            IsTerminated = true;
        }
    }
}
