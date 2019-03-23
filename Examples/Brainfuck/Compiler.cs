using System.Collections.Generic;
using Flame;
using Flame.Compiler;
using Pixie.Code;
using Pixie;
using Flame.TypeSystem;
using System.Linq;
using Flame.Clr;
using Flame.Constants;
using Flame.Compiler.Instructions;
using Pixie.Markup;
using Flame.Compiler.Flow;
using Flame.Collections;
using Flame.Clr.Emit;
using Flame.Compiler.Analysis;
using Flame.Compiler.Transforms;
using Pixie.Options;
using Flame.Ir;
using Loyc.Syntax.Les;
using Loyc.Syntax;

namespace Flame.Brainfuck
{
    public sealed class SourceReader
    {
        public SourceReader(SourceDocument document)
        {
            this.Document = document;
            this.Code = document.GetText(0, document.Length);
            this.Position = 0;
        }

        public string Code { get; private set; }
        public SourceDocument Document { get; private set; }
        public int Position;

        public bool IsEmpty
        {
            get { return Position >= Code.Length; }
        }
        public char Previous
        {
            get
            {
                if (Position > 0 && Position <= Code.Length)
                    return Code[Position - 1];
                else
                    return default(char);
            }
        }

        public MarkupNode Highlight(int position, int length)
        {
            return new HighlightedSource(
                new SourceRegion(
                    new SourceSpan(Document, position, length)));
        }
    }

    public sealed class Compiler
    {
        public Compiler(ClrAssembly assembly, Dependencies dependencies, ILog Log, OptionSet options)
        {
            this.Assembly = assembly;
            this.Log = Log;
            this.Dependencies = dependencies;
            this.CompilerOptions = options;
        }

        public ClrAssembly Assembly { get; private set; }
        public ILog Log { get; private set; }
        public Dependencies Dependencies { get; private set; }
        public OptionSet CompilerOptions { get; private set; }

        public TypeEnvironment Environment => Dependencies.Environment;

        /// <summary>
        /// Compiles Brainfuck source code down to a method and sets the
        /// assembly's entry point to that method.
        /// </summary>
        /// <param name="document">
        /// A Brainfuck source code document.
        /// </param>
        public void Compile(SourceDocument document)
        {
            // Compile the Brainfuck source code to a Flame IR method body.
            var sourceBody = CompileBody(new SourceReader(document));

            // Optimize the IR method body.
            var body = sourceBody.WithImplementation(
                sourceBody.Implementation.Transform(
                    AllocaToRegister.Instance,
                    CopyPropagation.Instance,
                    new ConstantPropagation(),
                    GlobalValueNumbering.Instance,
                    CopyPropagation.Instance,
                    InstructionSimplification.Instance,
                    DeadValueElimination.Instance,
                    MemoryAccessElimination.Instance,
                    CopyPropagation.Instance,
                    new ConstantPropagation(),
                    DeadValueElimination.Instance,
                    ReassociateOperators.Instance,
                    DeadValueElimination.Instance,
                    FuseMemoryAccesses.Instance));

            if (CompilerOptions.GetValue<bool>(Options.PrintIr))
            {
                PrintIr(sourceBody, body);
            }

            // Define a class.
            var program = new Mono.Cecil.TypeDefinition(
                "Brainfuck",
                "Program",
                Mono.Cecil.TypeAttributes.Class | Mono.Cecil.TypeAttributes.Public,
                Assembly.Definition.MainModule.ImportReference(Environment.Object));

            Assembly.Definition.MainModule.Types.Add(program);

            // Add an entry point method to that class.
            var main = new Mono.Cecil.MethodDefinition(
                "Main",
                Mono.Cecil.MethodAttributes.Static | Mono.Cecil.MethodAttributes.Public,
                Assembly.Definition.MainModule.ImportReference(Environment.Void));

            program.Methods.Add(main);

            // Compile the method body down to CIL and assign it to the method.
            var emitter = new ClrMethodBodyEmitter(main, body, Environment);
            main.Body = emitter.Compile();

            // Set the entry point.
            Assembly.Definition.MainModule.EntryPoint = main;
        }

        /// <summary>
        /// Compiles Brainfuck source code down to a method body.
        /// </summary>
        /// <param name="reader">
        /// A source reader for Brainfuck source code.
        /// </param>
        /// <returns>
        /// A method body.
        /// </returns>
        private MethodBody CompileBody(SourceReader reader)
        {
            // Create a control-flow graph that consists of an entry point only.
            var graph = new FlowGraphBuilder();

            // Use a permissive exception delayability model to make the optimizer's
            // life easier.
            graph.AddAnalysis(
                new ConstantAnalysis<ExceptionDelayability>(
                    PermissiveExceptionDelayability.Instance));

            // Grab the entry point block.
            var block = graph.EntryPoint;

            // Allocate an array of Brainfuck cells, which we'll represent using
            // 8-bit unsigned integers (i.e., unsigned bytes).
            var cellCount = block.AppendInstruction(
                Instruction.CreateConstant(
                    new IntegerConstant(30000, IntegerSpec.Int32),
                    Environment.Int32));

            var cells = block.AppendInstruction(
                Instruction.CreateNewArrayIntrinsic(
                    Environment.MakeArrayType(Environment.UInt8, 1),
                    Environment.Int32,
                    cellCount));

            // Allocate a stack variable that stores an index into the array.
            var indexAlloca = block.AppendInstruction(
                Instruction.CreateAlloca(Environment.Int32));

            // Initially set that variable to one.
            block.AppendInstruction(
                Instruction.CreateStore(
                    Environment.Int32,
                    indexAlloca,
                    block.AppendInstruction(
                        Instruction.CreateConstant(
                            new IntegerConstant(1, IntegerSpec.Int32),
                            Environment.Int32))));

            // We now iterate through the Brainfuck source code and turn it into
            // instructions.
            var whileHeaders = new Stack<BasicBlockBuilder>();
            var whileTerminators = new Stack<BasicBlockBuilder>();
            while (!reader.IsEmpty)
            {
                char item = reader.Code[reader.Position];
                if (item == '>')
                {
                    IncrementOrDecrement(indexAlloca, block, ArithmeticIntrinsics.Operators.Add);
                }
                else if (item == '<')
                {
                    IncrementOrDecrement(indexAlloca, block, ArithmeticIntrinsics.Operators.Subtract);
                }
                else if (item == '+')
                {
                    IncrementOrDecrement(
                        GetDataPointer(Environment.UInt8, cells, indexAlloca, block),
                        block,
                        ArithmeticIntrinsics.Operators.Add);
                }
                else if (item == '-')
                {
                    IncrementOrDecrement(
                        GetDataPointer(Environment.UInt8, cells, indexAlloca, block),
                        block,
                        ArithmeticIntrinsics.Operators.Subtract);
                }
                else if (item == '[')
                {
                    var loopHeader = graph.AddBasicBlock();
                    var loopBody = graph.AddBasicBlock();
                    var loopTerminator = graph.AddBasicBlock();

                    whileHeaders.Push(loopHeader);
                    whileTerminators.Push(loopTerminator);

                    var dataPtr = GetDataPointer(Environment.UInt8, cells, indexAlloca, block);
                    loopHeader.Flow = SwitchFlow.CreateConstantCheck(
                        Instruction.CreateLoad(Environment.UInt8, dataPtr),
                        new IntegerConstant(0, IntegerSpec.UInt8),
                        new Branch(loopTerminator),
                        new Branch(loopBody));

                    block.Flow = new JumpFlow(loopHeader);

                    block = loopBody;

                    if (reader.IsEmpty)
                    {
                        Log.Log(
                            new LogEntry(
                                Severity.Error,
                                "loop not closed",
                                "a loop was opened with '[', but not closed with ']'.",
                                reader.Highlight(reader.Position, 1)));
                    }
                }
                else if (item == ']')
                {
                    if (whileHeaders.Count == 0)
                    {
                        Log.Log(
                            new LogEntry(
                                Severity.Warning,
                                "program closed",
                                "the program was closed by ']'. This is an fbfc compiler extension.",
                                reader.Highlight(reader.Position, 1)));
                    }
                    var header = whileHeaders.Pop();
                    var term = whileTerminators.Pop();
                    block.Flow = new JumpFlow(header);
                    block = term;
                }
                else if (item == '.')
                {
                    var ptr = GetDataPointer(Environment.UInt8, cells, indexAlloca, block);
                    Dependencies.EmitWrite(
                        ref block,
                        block.AppendInstruction(Instruction.CreateLoad(Environment.UInt8, ptr)));
                }
                else if (item == ',')
                {
                    var ptr = GetDataPointer(Environment.UInt8, cells, indexAlloca, block);
                    block.AppendInstruction(
                        Instruction.CreateStore(
                            Environment.UInt8,
                            ptr,
                            Dependencies.EmitRead(ref block, Environment.UInt8)));
                }
                reader.Position++;
            }

            // Terminate the block with a 'return void' flow.
            block.Flow = new ReturnFlow(
                Instruction.CreateConstant(DefaultConstant.Instance, Environment.Void));

            // Finish up the method body.
            return new MethodBody(
                new Parameter(Environment.Void),
                default(Parameter),
                EmptyArray<Parameter>.Value,
                graph.ToImmutable());
        }

        private static ValueTag GetDataPointer(
            IType elementType,
            ValueTag cellArray,
            ValueTag cellIndexAlloca,
            BasicBlockBuilder block)
        {
            var arrayType = block.Graph.GetValueType(cellArray);
            var indexAllocType = block.Graph.GetValueType(cellIndexAlloca);
            var indexType = ((PointerType)indexAllocType).ElementType;

            return block.AppendInstruction(
                Instruction.CreateGetElementPointerIntrinsic(
                    elementType,
                    arrayType,
                    new[] { indexType },
                    cellArray,
                    new ValueTag[]
                    {
                        block.AppendInstruction(
                            Instruction.CreateLoad(indexType, cellIndexAlloca))
                    }));
        }

        /// <summary>
        /// Applies a binary arithmetic intrinsic to an integer value stored
        /// at an address and a 'one' integer constant.
        /// </summary>
        /// <param name="pointer">
        /// An address that contains an integer variable to update.
        /// </param>
        /// <param name="block">
        /// The block to write the update instructions to.
        /// </param>
        /// <param name="op">
        /// The operator to apply.
        /// </param>
        private static void IncrementOrDecrement(
            ValueTag pointer,
            BasicBlockBuilder block,
            string op)
        {
            var integerType = ((PointerType)block.Graph.GetValueType(pointer)).ElementType;
            block.AppendInstruction(
                Instruction.CreateStore(
                    integerType,
                    pointer,
                    block.AppendInstruction(
                        Instruction.CreateBinaryArithmeticIntrinsic(
                            op,
                            integerType,
                            block.AppendInstruction(
                                Instruction.CreateLoad(
                                    integerType,
                                    pointer)),
                            block.AppendInstruction(
                                Instruction.CreateConstant(
                                    new IntegerConstant(1, integerType.GetIntegerSpecOrNull()),
                                    integerType))))));
        }

        private void PrintIr(
            MethodBody sourceBody,
            MethodBody optBody)
        {
            var sourceIr = FormatIr(sourceBody);
            var optIr = FormatIr(optBody);

            Log.Log(
                new LogEntry(
                    Severity.Message,
                    "method body IR",
                    "optimized Flame IR: ",
                    new Paragraph(new WrapBox(optIr, 0, -optIr.Length)),
                    CreateRemark(
                        "unoptimized Flame IR:",
                        new Paragraph(new WrapBox(sourceIr, 0, -sourceIr.Length)))));
        }

        private static string FormatIr(MethodBody methodBody)
        {
            var encoder = new EncoderState();
            var encodedImpl = encoder.Encode(methodBody.Implementation);

            return Les2LanguageService.Value.Print(
                encodedImpl,
                options: new LNodePrinterOptions
                {
                    IndentString = new string(' ', 4)
                });
        }

        private static MarkupNode CreateRemark(
            params MarkupNode[] contents)
        {
            return new Paragraph(
                new MarkupNode[] { DecorationSpan.MakeBold(new ColorSpan("remark: ", Colors.Gray)) }
                .Concat(contents)
                .ToArray());
        }
    }
}