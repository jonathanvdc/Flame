using System.Collections.Generic;
using Flame;
using Flame.Compiler;
using Pixie.Code;
using Pixie;
using Flame.TypeSystem;
using System.Linq;
using Flame.Clr;

namespace Flame.Brainfuck
{
    public class SourceReader
    {
        public SourceReader(SourceDocument Document)
        {
            this.Document = Document;
            this.Code = Document.GetText(0, Document.Length);
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
    }

    public class BrainfuckEnvironment
    {
        public BrainfuckEnvironment(ValueTag ArrayVariable, ValueTag PointerVariable)
        {
            this.ArrayVariable = ArrayVariable;
            this.PointerVariable = PointerVariable;
        }

        public ValueTag ArrayVariable { get; private set; }
        public ValueTag PointerVariable { get; private set; }

        public IVariable CreateDataVariable()
        {
            return new ElementVariable(ArrayVariable.CreateGetExpression(), new IExpression[] { PointerVariable.CreateGetExpression() });
        }
    }

    public class Compiler
    {
        public Compiler(ClrAssembly assembly, ILog Log, IMethod ReadMethod, IMethod WriteMethod)
        {
            this.assembly = assembly;
            this.Log = Log;
            this.ReadMethod = ReadMethod;
            this.WriteMethod = WriteMethod;
        }

        public Compiler(ClrAssembly assembly, ILog Log, TypeEnvironment environment, ReadOnlyTypeResolver Binder)
        {
            this.assembly = assembly;
            this.Log = Log;

            var consoleType = Binder.ResolveTypes(new SimpleName("Console").Qualify("System")).FirstOrDefault();
            if (consoleType == null)
            {
                Log.Log(
                    new LogEntry(
                        Severity.Warning,
                        "console not found",
                        "no class named 'System.Console' was not found. IO calls will be replaced with constants."));
            }
            else
            {
                WriteMethod = consoleType.Methods.FirstOrDefault(
                    method => method.Name.ToString() == "Write"
                        && method.IsStatic
                        && method.ReturnParameter.Type == environment.Void
                        && method.Parameters.Count == 1
                        && method.Parameters[0].Type == environment.Char);

                ReadMethod = consoleType.Methods.FirstOrDefault(
                    method => method.Name.ToString() == "Read"
                        && method.IsStatic
                        && method.ReturnParameter.Type == environment.Int32
                        && method.Parameters.Count == 0); 

                if (WriteMethod != null)
                {
                    Log.Log(
                        new LogEntry(
                            Severity.Info,
                            "output method found",
                            "found 'void " + WriteMethod.FullName + "(" + WriteMethod.Parameters[0].Type.Name + ")'."));
                }
                else
                {
                    Log.Log(
                        new LogEntry(
                            Severity.Warning,
                            "output method not found",
                            "couldn't find 'void System.Console.Write(char)'. No output will be written."));
                }
                if (ReadMethod != null)
                {
                    Log.Log(
                        new LogEntry(
                            Severity.Info,
                            "input method found",
                            "found 'int System.Console.Read()'."));
                }
                else
                {
                    Log.Log(
                        new LogEntry(
                            Severity.Warning,
                            "input method not found",
                            "couldn't find 'char System.Console.Read()'. No input will be read."));
                }
            }
        }

        public ClrAssembly Assembly { get; private set; }
        public ILog Log { get; private set; }

        public IMethod WriteMethod { get; private set; }
        public IMethod ReadMethod { get; private set; }

        public IStatement CreateWriteStatement(IExpression Value)
        {
            if (WriteMethod == null)
                return new EmptyStatement();

            return new ExpressionStatement(new InvocationExpression(WriteMethod, null,
                new IExpression[] {
                    new ConversionExpression(Value, WriteMethod.GetParameters()[0].ParameterType)
                }));
        }

        private IExpression readExpr;
        public IExpression CreateReadExpression(IType TargetType)
        {
            if (readExpr == null)
            {
                if (ReadMethod == null)
                    readExpr = new ConversionExpression(new Int8Expression(0), TargetType);

                var call = new InvocationExpression(ReadMethod, null, new IExpression[] { });

                if (ReadMethod.ReturnType.IsSignedInteger)
                {
                    var resultVariable = new LateBoundVariable("temp", ReadMethod.ReturnType);
                    var resultSet = resultVariable.CreateSetStatement(call);
                    var resultExpr = resultVariable.CreateGetExpression();
                    var zero = new ConversionExpression(new Int8Expression(0), ReadMethod.ReturnType);
                    var selectExpr = new SelectExpression(new GreaterThanExpression(resultExpr, zero), resultExpr, zero);
                    return new InitializedExpression(resultSet, selectExpr); // Do not release the temporary as we are already reusing it
                }
                else
                    readExpr = new ConversionExpression(call, TargetType);
            }
            return readExpr;
        }

        public IAssembly Compile(string Name, SourceDocument Code)
        {
            return ToAssembly(Name, CompileBody(new SourceReader(Code)));
        }

        public IAssembly ToAssembly(string Name, IStatement Statement)
        {
            var descAssembly = new DescribedAssembly(Name);
            var descType = new DescribedType("Program", descAssembly);
            var descMethod = new DescribedBodyMethod("Main", descType, PrimitiveTypes.Void, true);
            descMethod.Body = Statement;
            descType.AddMethod(descMethod);
            descAssembly.AddType(descType);
            descAssembly.EntryPoint = descMethod;
            return descAssembly;
        }

        public IStatement CompileBody(SourceReader Reader)
        {
            int cellCount = Log.Options.GetOption<int>("cell-count", 30000); // Arbitrary array size

            var elemType = PrimitiveTypes.UInt8;
            var arrType = elemType.MakeArrayType(1); // uint8[]
            var arrVar = new LateBoundVariable("data", arrType);
            var arrCreation = arrVar.CreateSetStatement(new NewArrayExpression(elemType, new IExpression[] { new Int32Expression(cellCount) }));

            var ptrVar = new LateBoundVariable("ptr", PrimitiveTypes.Int32);
            var ptrInit = ptrVar.CreateSetStatement(new Int32Expression(0));

            var env = new BrainfuckEnvironment(arrVar, ptrVar);

            var body = CompileBody(Reader, env);

            if (!Reader.IsEmpty)
            {
                Log.LogWarning(new LogEntry("Program closed", "The program was closed by ']'. This is a compiler extension.", new SourceLocation(Reader.Document, Reader.Position, 1)));
            }

            var ret = new ReturnStatement();

            return new BlockStatement(new IStatement[] { arrCreation, ptrInit, body, ret });
        }

        private IStatement CompileBody(SourceReader Reader, BrainfuckEnvironment Environment)
        {
            var stmts = new List<IStatement>();

            while (!Reader.IsEmpty)
            {
                char item = Reader.Code[Reader.Position];
                if (item == ']')
                {
                    break; // This signifies the end of a while loop. Assume we're parsing said while loop's body.
                }

                if (item == '>')
                {
                    stmts.Add(Environment.PointerVariable.CreateSetStatement(new AddExpression(Environment.PointerVariable.CreateGetExpression(), new Int32Expression(1))));
                }
                else if (item == '<')
                {
                    stmts.Add(Environment.PointerVariable.CreateSetStatement(new SubtractExpression(Environment.PointerVariable.CreateGetExpression(), new Int32Expression(1))));
                }
                else if (item == '+')
                {
                    var elem = Environment.CreateDataVariable();
                    stmts.Add(elem.CreateSetStatement(new AddExpression(elem.CreateGetExpression(), new Int32Expression(1))));
                }
                else if (item == '-')
                {
                    var elem = Environment.CreateDataVariable();
                    stmts.Add(elem.CreateSetStatement(new SubtractExpression(elem.CreateGetExpression(), new Int32Expression(1))));
                }
                else if (item == '[')
                {
                    int pos = Reader.Position;
                    Reader.Position++;
                    var elem = Environment.CreateDataVariable();
                    var cond = new InequalityExpression(elem.CreateGetExpression(), new Int32Expression(0));
                    var body = CompileBody(Reader, Environment);
                    if (Reader.IsEmpty)
                    {
                        Log.LogError(new LogEntry("Loop not closed", "A loop was opened with '[', but not closed with ']'. Consider closing it.", new SourceLocation(Reader.Document, pos, 1)));
                    }
                    stmts.Add(new WhileStatement(cond, body));
                }
                else if (item == '.')
                {
                    var elem = Environment.CreateDataVariable();
                    stmts.Add(CreateWriteStatement(elem.CreateGetExpression()));
                }
                else if (item == ',')
                {
                    var elem = Environment.CreateDataVariable();
                    stmts.Add(elem.CreateSetStatement(CreateReadExpression(elem.Type)));
                }
                Reader.Position++;
            }

            return new BlockStatement(stmts);
        }
    }
}