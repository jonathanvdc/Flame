using System.Linq;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Constants;
using Flame.TypeSystem;
using Pixie;

namespace Flame.Brainfuck
{
    /// <summary>
    /// Dependencies that Brainfuck programs use to accept input and produce output.
    /// </summary>
    public struct Dependencies
    {
        /// <summary>
        /// Creates a Brainfuck IO dependencies data structure.
        /// </summary>
        /// <param name="environment">The type environment to use.</param>
        /// <param name="readMethod">The 'read' method to use.</param>
        /// <param name="writeMethod">The 'write' method to use.</param>
        public Dependencies(TypeEnvironment environment, IMethod readMethod, IMethod writeMethod)
        {
            this.Environment = environment;
            this.ReadMethod = readMethod;
            this.WriteMethod = writeMethod;
        }

        /// <summary>
        /// Gets the type environment.
        /// </summary>
        /// <value>The type environment.</value>
        public TypeEnvironment Environment { get; private set; }

        /// <summary>
        /// Gets the 'read' method, which reads a character from the input stream.
        /// </summary>
        /// <value>The 'read' method.</value>
        public IMethod ReadMethod { get; private set; }

        /// <summary>
        /// Gets the 'write' method, which writes a character to the input stream.
        /// </summary>
        /// <value>The 'write' method.</value>
        public IMethod WriteMethod { get; private set; }

        /// <summary>
        /// Resolves dependencies that Brainfuck programs use to accept input and produce output.
        /// </summary>
        /// <param name="environment">
        /// The type environment to resolve dependencies in.
        /// </param>
        /// <param name="binder">
        /// A type resolver for resolving dependencies with.
        /// </param>
        /// <param name="log">A log to send messages to.</param>
        /// <returns>
        /// Brainfuck IO dependencies.
        /// </returns>
        public static Dependencies Resolve(
            TypeEnvironment environment,
            ReadOnlyTypeResolver binder,
            ILog log)
        {
            var consoleType = binder.ResolveTypes(new SimpleName("Console").Qualify("System")).FirstOrDefault();
            if (consoleType == null)
            {
                log.Log(
                    new LogEntry(
                        Severity.Warning,
                        "console not found",
                        "no class named 'System.Console' was not found. IO calls will be replaced with constants."));
                return new Dependencies(environment, null, null);
            }
            else
            {
                var writeMethod = consoleType.Methods.FirstOrDefault(
                    method => method.Name.ToString() == "Write"
                        && method.IsStatic
                        && method.ReturnParameter.Type == environment.Void
                        && method.Parameters.Count == 1
                        && method.Parameters[0].Type == environment.Char);

                var readMethod = consoleType.Methods.FirstOrDefault(
                    method => method.Name.ToString() == "Read"
                        && method.IsStatic
                        && method.ReturnParameter.Type == environment.Int32
                        && method.Parameters.Count == 0); 

                if (writeMethod != null)
                {
                    log.Log(
                        new LogEntry(
                            Severity.Info,
                            "output method found",
                            "found 'void System.Console.Write(char)'."));
                }
                else
                {
                    log.Log(
                        new LogEntry(
                            Severity.Warning,
                            "output method not found",
                            "couldn't find 'void System.Console.Write(char)'. No output will be written."));
                }
                if (readMethod != null)
                {
                    log.Log(
                        new LogEntry(
                            Severity.Info,
                            "input method found",
                            "found 'int System.Console.Read()'."));
                }
                else
                {
                    log.Log(
                        new LogEntry(
                            Severity.Warning,
                            "input method not found",
                            "couldn't find 'char System.Console.Read()'. No input will be read."));
                }

                return new Dependencies(environment, readMethod, writeMethod);
            }
        }

        /// <summary>
        /// Emits instructions that read a character from the input stream.
        /// </summary>
        /// <param name="block">A basic block builder.</param>
        /// <param name="resultType">A control-flow graph builder.</param>
        /// <returns>The character that is read from the input stream.</returns>
        public ValueTag EmitRead(ref BasicBlockBuilder block, IType resultType)
        {
            if (ReadMethod == null)
            {
                // If we didn't find a 'read' method, then we'll just return zero.
                return block.AppendInstruction(
                    Instruction.CreateConstant(
                        new IntegerConstant(0, resultType.GetIntegerSpecOrNull()),
                        resultType));
            }

            var returnValue = block.AppendInstruction(
                Instruction.CreateCall(
                    ReadMethod,
                    MethodLookup.Static,
                    EmptyArray<ValueTag>.Value));

            var returnType = returnValue.Instruction.ResultType;
            if (returnType == resultType)
            {
                return returnValue;
            }
            else if (returnType.IsSignedIntegerType() && !resultType.IsSignedIntegerType())
            {
                // When converting a signed return type to an unsigned result type,
                // we want to map negative values to zero because both represent an
                // end-of-stream marker but a direct conversion won't preserve those
                // semantics.

                // Create a zero constant with the same type as the return type.
                var returnZero = block.AppendInstruction(
                    Instruction.CreateConstant(
                        new IntegerConstant(0, returnType.GetIntegerSpecOrNull()),
                        returnType));

                // Compare the return value to zero.
                var lt = Instruction.CreateRelationalIntrinsic(
                    ArithmeticIntrinsics.Operators.IsLessThan,
                    returnType,
                    returnType,
                    returnValue,
                    returnZero);

                // Create a new basic block so we can set `block`'s flow to a switch.
                var successorBlock = block.Graph.AddBasicBlock();
                var resultParam = new BlockParameter(resultType);
                successorBlock.AppendParameter(resultParam);

                // Create an additional basic block that converts the return value to
                // the result type if the return value is positive.
                var convBlock = block.Graph.AddBasicBlock();

                // Convert the return value to the result type.
                var convReturnValue = block.AppendInstruction(
                    Instruction.CreateConvertIntrinsic(resultType, returnType, returnValue));

                // Set the conversion block's outgoing flow to jump to the successor block.
                convBlock.Flow = new JumpFlow(successorBlock, new ValueTag[] { convReturnValue });

                // Create a zero constant with the same type as the result type.
                var resultZero = block.AppendInstruction(
                    Instruction.CreateConstant(
                        new IntegerConstant(0, resultType.GetIntegerSpecOrNull()),
                        resultType));

                // Set the outgoing flow.
                block.Flow = SwitchFlow.CreateIfElse(
                    lt,
                    new Branch(successorBlock, new ValueTag[] { resultZero }),
                    new Branch(convBlock));

                // Update the block.
                block = successorBlock;

                // Return the value tag of the result parameter.
                return resultParam.Tag;
            }
            else
            {
                // Otherwise, just convert the result using a straightforward intrinsic.
                return block.AppendInstruction(
                    Instruction.CreateConvertIntrinsic(resultType, returnType, returnValue));
            }
        }

        /// <summary>
        /// Emits instructions that write a character to the output stream.
        /// </summary>
        /// <param name="block">A basic block builder.</param>
        /// <param name="character">The character to write to the output stream.</param>
        public void EmitWrite(ref BasicBlockBuilder block, ValueTag character)
        {
            if (WriteMethod == null)
            {
                // Do nothing if the 'write' method is null.
                return;
            }

            // Convert the character to a type that the 'write' method is okay with.
            var convChar = block.AppendInstruction(
                Instruction.CreateConvertIntrinsic(
                    WriteMethod.Parameters[0].Type,
                    block.Graph.GetValueType(character),
                    character));

            // Call the 'write' method.
            block.AppendInstruction(
                Instruction.CreateCall(
                    WriteMethod,
                    MethodLookup.Static,
                    new ValueTag[] { convChar }));
        }
    }
}
