using System;
using System.Collections.Generic;
using Loyc;
using Loyc.Collections;
using Loyc.MiniTest;
using System.Diagnostics;
using Loyc.Syntax;
using UnitTests.Flame.Ir;
using System.Collections.Immutable;
using Pixie;
using Pixie.Transforms;
using Pixie.Markup;
using UnitTests.Flame.Clr;
using UnitTests.Flame.Compiler;
using UnitTests.Macros;
using Pixie.Options;
using System.Linq;
using System.Globalization;

namespace UnitTests
{
    // Test driver based on Loyc project: https://github.com/qwertie/ecsharp/blob/master/Core/Tests/Program.cs

    public class Program
    {
        public static readonly List<Pair<string, Func<int>>> Menu = new List<Pair<string, Func<int>>>()
        {
            new Pair<string,Func<int>>("Run unit tests of Flame.dll", Flame),
            new Pair<string,Func<int>>("Run unit tests of Flame.Clr.dll", FlameClr),
            new Pair<string,Func<int>>("Run unit tests of Flame.Compiler.dll", FlameCompiler),
            new Pair<string,Func<int>>("Run unit tests of Flame.Ir.dll", FlameIr),
            new Pair<string,Func<int>>("Run unit tests of FlameMacros.dll", FlameMacros),
            new Pair<string,Func<int>>("Run Flame tool tests", FlameTools)
        };

        public static int Main(string[] args)
        {
            InitializeLogs();
            // Parse command-line options.
            var parser = new GnuOptionSetParser(
                Options.All, Options.Input);

            var recLog = new RecordingLog(ioLog);
            parsedOptions = parser.Parse(args, recLog);

            if (recLog.Contains(Pixie.Severity.Error))
            {
                // Stop the program if the command-line arguments
                // are half baked. The parser will report an error.
                return 1;
            }

            if (parsedOptions.GetValue<bool>(Options.Help))
            {
                // Wrap the help message into a log entry and send it to the log.
                rawLog.Log(
                    new LogEntry(
                        Pixie.Severity.Info,
                        new HelpMessage(
                            "unit-tests is a command-line tool that runs Flame's unit tests.",
                            "unit-tests [all|0|1|2|3|4|5|6...] [options...]",
                            Options.All)));
                return 0;
            }

            var positionalArgs = parsedOptions
                .GetValue<IEnumerable<string>>(Options.Input)
                .ToArray();

            if (positionalArgs.Length == 0)
            {
                // Workaround for MS bug: Assert(false) will not fire in debugger
                Debug.Listeners.Clear();
                Debug.Listeners.Add(new DefaultTraceListener());
                if (RunMenu(Menu) > 0)
                    // Let the outside world know that something went wrong (e.g. Travis CI)
                    return 1;
                else
                    return 0;
            }
            else
            {
                int errorCount = 0;
                foreach (var arg in positionalArgs)
                {
                    if (arg.Equals("all", StringComparison.InvariantCultureIgnoreCase))
                    {
                        errorCount += RunAllTests(Menu);
                    }
                    else
                    {
                        int testIndex;
                        if (int.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out testIndex)
                            && testIndex > 0 && testIndex <= Menu.Count)
                        {
                            errorCount += errorCount += Menu[testIndex - 1].Value();
                        }
                        else
                        {
                            ioLog.Log(
                                new LogEntry(
                                    Pixie.Severity.Error,
                                    "ill-formed positional argument",
                                    $"positional argument '{arg} does not name a test suite.'"));
                            errorCount += 1;
                        }
                    }
                }
                return errorCount == 0 ? 0 : 1;
            }
        }

        public static int RunMenu(List<Pair<string, Func<int>>> menu)
        {
            int errorCount = 0;
            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("What do you want to do? (Esc to quit)");
                for (int i = 0; i < menu.Count; i++)
                    Console.WriteLine(PrintHelpers.HexDigitChar(i + 1) + ". " + menu[i].Key);
                Console.WriteLine("Space. Run all tests");

                char c = default(char);
                for (ConsoleKeyInfo k; (k = Console.ReadKey(true)).Key != ConsoleKey.Enter;)
                {
                    if (k.Key == ConsoleKey.Escape)
                    {
                        return errorCount;
                    }
                    else
                    {
                        c = k.KeyChar;
                        break;
                    }
                }

                if (c == ' ')
                {
                    errorCount += RunAllTests(menu);
                }
                else
                {
                    int i = ParseHelpers.HexDigitValue(c);
                    if (i > 0 && i <= menu.Count)
                        errorCount += menu[i - 1].Value();
                }
            }
        }

        private static int RunAllTests(List<Pair<string, Func<int>>> menu)
        {
            int errorCount = 0;
            for (int i = 0; i < menu.Count; i++)
            {
                errorCount += menu[i].Value();
            }
            return errorCount;
        }

        private static Random globalRng = new Random();

        private static ILog rawLog;

        private static ILog ioLog;

        private static ILog testLog;

        private static void InitializeLogs()
        {
            rawLog = Pixie.Terminal.TerminalLog.Acquire();
            ioLog = new TransformLog(
                rawLog,
                entry => DiagnosticExtractor.Transform(entry, new Text("unit-tests")));
            testLog = new TestLog(
                ImmutableHashSet<Pixie.Severity>.Empty.Add(Pixie.Severity.Error),
                ioLog);
        }

        internal static OptionSet parsedOptions;

        public static int Flame()
        {
            return RunTests.RunMany(
                new AssemblyIdentityTests(globalRng),
                new CacheTests(globalRng),
                new DeferredInitializerTests(),
                new IndexTests(globalRng),
                new IntegerConstantTests(),
                new QualifiedNameTests(),
                new SmallMultiDictionaryTests(),
                new SymmetricRelationTests(),
                new TypeConstructionTests(globalRng),
                new TypeResolverTests(),
                new ValueListTests());
        }

        public static int FlameClr()
        {
            return RunTests.RunMany(
                new CilAnalysisTests(testLog),
                new CilEmitTests(testLog),
                new LocalTypeResolutionTests(),
                new MemberResolutionTests(),
                new NameConversionTests(),
                new TypeAttributeTests());
        }

        public static int FlameCompiler()
        {
            return RunTests.RunMany(
                new ArithmeticIntrinsicsTests(),
                new DominatorTreeAnalysisTests(),
                new FlowGraphAnalysisTests(),
                new FlowGraphTests(),
                new InterferenceGraphAnalysisTests(),
                new LivenessAnalysisTests(),
                new OptimizerTests(testLog),
                new PredecessorAnalysisTests(),
                new RelatedValueAnalysisTests(),
                new ValueUseAnalysisTests());
        }

        public static int FlameIr()
        {
            return RunTests.RunMany(
                new AssemblyCodecTest(testLog),
                new ConstantCodecTest(testLog, globalRng),
                new PiecewiseCodecTest(testLog),
                new TypeCodecTest(testLog));
        }

        public static int FlameMacros()
        {
            return RunTests.RunMany(
                new InstructionPatternTests());
        }

        public static int FlameTools()
        {
            return RunTests.RunMany(
                new BrainfuckTests(),
                new ILOptTests());
        }
    }
}
