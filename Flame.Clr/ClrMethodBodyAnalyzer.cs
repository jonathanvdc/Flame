using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Constants;
using Flame.TypeSystem;

namespace Flame.Clr
{
    /// <summary>
    /// A data structure that analyzes CIL instructions
    /// and translates them to Flame IR.
    /// </summary>
    public sealed class ClrMethodBodyAnalyzer
    {
        /// <summary>
        /// Creates a method body analyzer.
        /// </summary>
        /// <param name="returnParameter">
        /// The 'return' parameter of the method body.
        /// </param>
        /// <param name="thisParameter">
        /// The 'this' parameter of the method body.
        /// </param>
        /// <param name="parameters">
        /// The parameter list of the method body.
        /// </param>
        /// <param name="assembly">
        /// A reference to the assembly that defines the
        /// method body.
        /// </param>
        private ClrMethodBodyAnalyzer(
            Parameter returnParameter,
            Parameter thisParameter,
            IReadOnlyList<Parameter> parameters,
            ClrAssembly assembly)
        {
            this.ReturnParameter = returnParameter;
            this.ThisParameter = thisParameter;
            this.Parameters = parameters;
            this.Assembly = assembly;
            this.graph = new FlowGraphBuilder();
        }

        /// <summary>
        /// Gets the 'return' parameter of the method body.
        /// </summary>
        /// <returns>The 'return' parameter.</returns>
        public Parameter ReturnParameter { get; private set; }

        /// <summary>
        /// Gets the 'this' parameter of the method body.
        /// </summary>
        /// <returns>The 'this' parameter.</returns>
        public Parameter ThisParameter { get; private set; }

        /// <summary>
        /// Gets the parameter list of the method body.
        /// </summary>
        /// <returns>The parameter list.</returns>
        public IReadOnlyList<Parameter> Parameters { get; private set; }

        /// <summary>
        /// Gets a reference to the assembly that defines the
        /// method body.
        /// </summary>
        /// <returns>An assembly reference.</returns>
        public ClrAssembly Assembly { get; private set; }

        // The flow graph being constructed by this method body
        // analyzer.
        private FlowGraphBuilder graph;

        private Dictionary<Mono.Cecil.Cil.Instruction, BasicBlockBuilder> branchTargets;
        private HashSet<BasicBlockBuilder> analyzedBlocks;
        private List<InstructionBuilder> parameterStackSlots;
        private List<InstructionBuilder> localStackSlots;

        /// <summary>
        /// Analyzes a particular method body.
        /// </summary>
        /// <param name="cilMethodBody">
        /// The CIL method body to analyze.
        /// </param>
        /// <param name="method">
        /// The method that defines the method body.
        /// </param>
        /// <returns>
        /// A Flame IR method body.
        /// </returns>
        public static MethodBody Analyze(
            Mono.Cecil.Cil.MethodBody cilMethodBody,
            ClrMethodDefinition method)
        {
            return Analyze(
                cilMethodBody,
                method.ReturnParameter,
                cilMethodBody.ThisParameter == null
                    ? default(Parameter)
                    : ClrMethodDefinition.WrapParameter(
                        cilMethodBody.ThisParameter,
                        method.ParentType.Assembly,
                        method),
                method.Parameters,
                method.ParentType.Assembly);
        }

        /// <summary>
        /// Analyzes a particular method body.
        /// </summary>
        /// <param name="cilMethodBody">
        /// The CIL method body to analyze.
        /// </param>
        /// <param name="returnParameter">
        /// The 'return' parameter of the method body.
        /// </param>
        /// <param name="thisParameter">
        /// The 'this' parameter of the method body.
        /// </param>
        /// <param name="parameters">
        /// The parameter list of the method body.
        /// </param>
        /// <param name="assembly">
        /// A reference to the assembly that defines the
        /// method body.
        /// </param>
        /// <returns>
        /// A Flame IR method body.
        /// </returns>
        public static MethodBody Analyze(
            Mono.Cecil.Cil.MethodBody cilMethodBody,
            Parameter returnParameter,
            Parameter thisParameter,
            IReadOnlyList<Parameter> parameters,
            ClrAssembly assembly)
        {
            var analyzer = new ClrMethodBodyAnalyzer(
                returnParameter,
                thisParameter,
                parameters,
                assembly);

            // Simplify macros so we don't have to deal with as many
            // instructions.
            // FIXME: this modifies `cilMethodBody` in place, which is
            // arguably bad.
            Mono.Cecil.Rocks.MethodBodyRocks.SimplifyMacros(cilMethodBody);

            // Analyze branch targets so we'll know which instructions
            // belong to which basic blocks.
            analyzer.AnalyzeBranchTargets(cilMethodBody);

            if (cilMethodBody.Instructions.Count > 0)
            {
                // Create an entry point that sets up stack slots
                // for the method body's parameters and locals.
                analyzer.CreateEntryPoint(cilMethodBody);

                // Analyze the entire flow graph by starting at the
                // entry point block.
                analyzer.AnalyzeBlock(
                    cilMethodBody.Instructions[0],
                    EmptyArray<IType>.Value);
            }

            return new MethodBody(
                analyzer.ReturnParameter,
                analyzer.ThisParameter,
                analyzer.Parameters,
                analyzer.graph.ToImmutable());
        }

        private void AnalyzeBlock(
            Mono.Cecil.Cil.Instruction firstInstruction,
            IReadOnlyList<IType> argumentTypes)
        {
            // Mark the block as analyzed so we don't analyze it
            // ever again. Be sure to check that the types of
            // the arguments the block receives are the same if
            // the block is already analyzed.
            var block = branchTargets[firstInstruction];
            if (!analyzedBlocks.Add(block))
            {
                var parameterTypes = block.Parameters
                    .Select(param => param.Type);
                bool sameParameters = parameterTypes
                    .SequenceEqual(argumentTypes);
                if (sameParameters)
                {
                    return;
                }
                else
                {
                    throw new InvalidProgramException(
                        "Different paths to instruction '" + firstInstruction.ToString() +
                        "' have incompatible stack contents.");
                }
            }

            // Set up block parameters.
            block.Parameters = argumentTypes
                .Select((type, index) =>
                    new BlockParameter(type, block.Tag.Name + "_stackarg_" + index))
                .ToImmutableList();

            var currentInstruction = firstInstruction;
            var stackContents = new Stack<ValueTag>(
                block.Parameters
                    .Select(param => param.Tag)
                    .Reverse());

            while (true)
            {
                // Analyze the current instruction.
                AnalyzeInstruction(currentInstruction, block, stackContents);
                if (currentInstruction.Next == null ||
                    branchTargets.ContainsKey(currentInstruction.Next))
                {
                    // Current instruction is the last instruction of the block.
                    return;
                }
                else
                {
                    // Current instruction is not the last instruction of the
                    // block. Proceed to the next instruction.
                    currentInstruction = currentInstruction.Next;
                }
            }
        }

        private static void PushValue(
            Instruction value,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            var instruction = block.AppendInstruction(value);
            stackContents.Push(instruction.Tag);
        }

        private static void LoadValue(
            ValueTag pointer,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            PushValue(
                LoadPrototype.Create(
                    ((PointerType)block.Graph.GetValueType(pointer)).ElementType)
                    .Instantiate(pointer),
                block,
                stackContents);
        }

        private static void StoreValue(
            ValueTag pointer,
            ValueTag value,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            PushValue(
                StorePrototype.Create(block.Graph.GetValueType(value))
                    .Instantiate(pointer, value),
                block,
                stackContents);
        }

        /// <summary>
        /// Emits a binary arithmetic intrinsic operation that
        /// has its first argument type as result type.
        /// </summary>
        /// <param name="operatorName">The name of the operator to create.</param>
        /// <param name="block">The block to update.</param>
        /// <param name="stackContents">The stack contents.</param>
        private static void EmitSignedArithmeticBinary(
            string operatorName,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            var first = stackContents.Pop();
            var second = stackContents.Pop();
            var firstType = block.Graph.GetValueType(first);
            var secondType = block.Graph.GetValueType(second);

            PushValue(
                ArithmeticIntrinsics.CreatePrototype(operatorName, firstType, firstType, secondType)
                    .Instantiate(new[] { first, second }),
                block,
                stackContents);
        }

        private void AnalyzeInstruction(
            Mono.Cecil.Cil.Instruction instruction,
            BasicBlockBuilder block,
            Stack<ValueTag> stackContents)
        {
            string opName;
            if (signedBinaryOperators.TryGetValue(instruction.OpCode, out opName))
            {
                EmitSignedArithmeticBinary(opName, block, stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldc_I4)
            {
                PushValue(
                    ConstantPrototype.Create(
                        new IntegerConstant((int)instruction.Operand),
                        Assembly.Resolver.TypeEnvironment.Int32)
                        .Instantiate(),
                    block,
                    stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldarga)
            {
                stackContents.Push(
                    parameterStackSlots[((Mono.Cecil.ParameterReference)instruction.Operand).Index].Tag);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldarg)
            {
                var alloca = parameterStackSlots[((Mono.Cecil.ParameterReference)instruction.Operand).Index];
                LoadValue(alloca.Tag, block, stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Starg)
            {
                var alloca = parameterStackSlots[((Mono.Cecil.ParameterReference)instruction.Operand).Index];
                StoreValue(alloca.Tag, stackContents.Pop(), block, stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldloca)
            {
                stackContents.Push(
                    localStackSlots[((Mono.Cecil.Cil.VariableReference)instruction.Operand).Index].Tag);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ldloc)
            {
                var alloca = localStackSlots[((Mono.Cecil.Cil.VariableReference)instruction.Operand).Index];
                LoadValue(alloca.Tag, block, stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Stloc)
            {
                var alloca = localStackSlots[((Mono.Cecil.Cil.VariableReference)instruction.Operand).Index];
                StoreValue(alloca.Tag, stackContents.Pop(), block, stackContents);
            }
            else if (instruction.OpCode == Mono.Cecil.Cil.OpCodes.Ret)
            {
                var value = stackContents.Pop();
                block.Flow = new ReturnFlow(
                    CopyPrototype.Create(graph.GetValueType(value))
                    .Instantiate(value));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private void AnalyzeBranchTargets(
            Mono.Cecil.Cil.MethodBody cilMethodBody)
        {
            branchTargets = new Dictionary<Mono.Cecil.Cil.Instruction, BasicBlockBuilder>();
            analyzedBlocks = new HashSet<BasicBlockBuilder>();
            if (cilMethodBody.Instructions.Count > 0)
            {
                FlagBranchTarget(cilMethodBody.Instructions[0]);
                foreach (var instruction in cilMethodBody.Instructions)
                {
                    AnalyzeBranchTargets(instruction);
                }
            }
        }

        private void AnalyzeBranchTargets(
            Mono.Cecil.Cil.Instruction cilInstruction)
        {
            if (cilInstruction.Operand is Mono.Cecil.Cil.Instruction)
            {
                FlagBranchTarget((Mono.Cecil.Cil.Instruction)cilInstruction.Operand);
                FlagBranchTarget(cilInstruction.Next);
            }
            else if (cilInstruction.Operand is Mono.Cecil.Cil.Instruction[])
            {
                foreach (var target in (Mono.Cecil.Cil.Instruction[])cilInstruction.Operand)
                {
                    FlagBranchTarget(target);
                }
                FlagBranchTarget(cilInstruction.Next);
            }
            else if (cilInstruction.OpCode == Mono.Cecil.Cil.OpCodes.Ret
                || cilInstruction.OpCode == Mono.Cecil.Cil.OpCodes.Throw
                || cilInstruction.OpCode == Mono.Cecil.Cil.OpCodes.Rethrow)
            {
                // Terminate the block defining the 'ret', 'throw' or 'rethrow'
                // by flagging the next block as a branch target.
                FlagBranchTarget(cilInstruction.Next);
            }
        }

        private void FlagBranchTarget(
            Mono.Cecil.Cil.Instruction target)
        {
            if (target != null && !branchTargets.ContainsKey(target))
            {
                branchTargets[target] = graph.AddBasicBlock(
                    "IL_" + target.Offset.ToString("X4"));
            }
        }

        /// <summary>
        /// Creates an entry point block that sets up stack
        /// slots for the method body's parameters and locals.
        /// </summary>
        /// <param name="cilMethodBody">
        /// The method body to create stack slots for.
        /// </param>
        private void CreateEntryPoint(Mono.Cecil.Cil.MethodBody cilMethodBody)
        {
            // Compose an extended parameter list by prepending the 'this'
            // parameter to the regular parameter list, provided that there
            // is a 'this' parameter.
            var extParameters = cilMethodBody.Method.HasThis
                ? new[] { ThisParameter }.Concat(Parameters).ToArray()
                : Parameters;

            // Grab the entry point block.
            var entryPoint = graph.GetBasicBlock(graph.EntryPointTag);

            // Create a block parameter in the entry point for each
            // actual parameter in the method.
            entryPoint.Parameters = extParameters
                .Select((param, index) =>
                    new BlockParameter(param.Type, param.Name.ToString()))
                .ToImmutableList();

            // For each parameter, allocate a stack slot and store the
            // value of the parameter in the stack slot.
            this.parameterStackSlots = new List<InstructionBuilder>();
            for (int i = 0; i < extParameters.Count; i++)
            {
                var param = extParameters[i];

                var alloca = entryPoint.AppendInstruction(
                    AllocaPrototype.Create(param.Type)
                        .Instantiate(),
                    new ValueTag(param.Name.ToString() + "_slot"));

                entryPoint.AppendInstruction(
                    StorePrototype.Create(param.Type)
                        .Instantiate(
                            alloca.Tag,
                            entryPoint.Parameters[i].Tag),
                    new ValueTag(param.Name.ToString()));

                this.parameterStackSlots.Add(alloca);
            }

            // For each local, allocate an empty stack slot.
            this.localStackSlots = new List<InstructionBuilder>();
            foreach (var local in cilMethodBody.Variables)
            {
                var alloca = entryPoint.AppendInstruction(
                    AllocaPrototype.Create(Assembly.Resolve(local.VariableType))
                        .Instantiate(),
                    new ValueTag("local_" + local.Index + "_slot"));

                this.localStackSlots.Add(alloca);
            }

            // Jump to the entry point instruction.
            entryPoint.Flow = new JumpFlow(
                branchTargets[cilMethodBody.Instructions[0]].Tag);
        }

        private static readonly IReadOnlyDictionary<Mono.Cecil.Cil.OpCode, string> signedBinaryOperators =
            new Dictionary<Mono.Cecil.Cil.OpCode, string>()
        {
            { Mono.Cecil.Cil.OpCodes.Add, ArithmeticIntrinsics.Operators.Add },
            { Mono.Cecil.Cil.OpCodes.Sub, ArithmeticIntrinsics.Operators.Subtract },
            { Mono.Cecil.Cil.OpCodes.Mul, ArithmeticIntrinsics.Operators.Multiply },
            { Mono.Cecil.Cil.OpCodes.Div, ArithmeticIntrinsics.Operators.Divide },
            { Mono.Cecil.Cil.OpCodes.Rem, ArithmeticIntrinsics.Operators.Remainder },
            { Mono.Cecil.Cil.OpCodes.Cgt, ArithmeticIntrinsics.Operators.IsGreaterThan },
            { Mono.Cecil.Cil.OpCodes.Ceq, ArithmeticIntrinsics.Operators.IsEqualTo },
            { Mono.Cecil.Cil.OpCodes.Clt, ArithmeticIntrinsics.Operators.IsLessThan },
            { Mono.Cecil.Cil.OpCodes.Not, ArithmeticIntrinsics.Operators.Not },
            { Mono.Cecil.Cil.OpCodes.Neg, ArithmeticIntrinsics.Operators.Not },
            { Mono.Cecil.Cil.OpCodes.And, ArithmeticIntrinsics.Operators.And },
            { Mono.Cecil.Cil.OpCodes.Or, ArithmeticIntrinsics.Operators.Or },
            { Mono.Cecil.Cil.OpCodes.Xor, ArithmeticIntrinsics.Operators.Xor }
        };
    }
}
