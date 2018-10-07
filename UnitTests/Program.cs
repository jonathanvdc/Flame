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
            new Pair<string,Func<int>>("Run Flame tool tests", FlameTools)
        };

        public static void Main(string[] args)
        {
            // Workaround for MS bug: Assert(false) will not fire in debugger
            Debug.Listeners.Clear();
            Debug.Listeners.Add(new DefaultTraceListener());
            if (RunMenu(Menu, args.Length > 0 ? args[0].GetEnumerator() : null) > 0)
                // Let the outside world know that something went wrong (e.g. Travis CI)
                Environment.ExitCode = 1;
        }

        public static int RunMenu(List<Pair<string, Func<int>>> menu, IEnumerator<char> input)
        {
            int errorCount = 0;
            for (;;)
            {
                Console.WriteLine();
                Console.WriteLine("What do you want to do? (Esc to quit)");
                for (int i = 0; i < menu.Count; i++)
                    Console.WriteLine(PrintHelpers.HexDigitChar(i + 1) + ". " + menu[i].Key);
                Console.WriteLine("Space. Run all tests");

                char c = default(char);
                if (input == null)
                {
                    for (ConsoleKeyInfo k; (k = Console.ReadKey(true)).Key != ConsoleKey.Escape
                        && k.Key != ConsoleKey.Enter;)
                    {
                        c = k.KeyChar;
                        break;
                    }
                }
                else
                {
                    if (!input.MoveNext())
                        break;

                    c = input.Current;
                }

                if (c == ' ')
                {
                    for (int i = 0; i < menu.Count; i++)
                    {
                        Console.WriteLine();
                        ConsoleMessageSink.WriteColoredMessage(ConsoleColor.White, i + 1, menu[i].Key);
                        errorCount += menu[i].Value();
                    }
                }
                else
                {
                    int i = ParseHelpers.HexDigitValue(c);
                    if (i > 0 && i <= menu.Count)
                        errorCount += menu[i - 1].Value();
                }
            }
            return errorCount;
        }

        private static Random globalRng = new Random();

        private static ILog globalLog = new TransformLog(
            new TestLog(
                ImmutableHashSet<Pixie.Severity>.Empty.Add(Pixie.Severity.Error),
                Pixie.Terminal.TerminalLog.Acquire()),
            entry => DiagnosticExtractor.Transform(entry, new Text("program")));

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
                new CilAnalysisTests(globalLog),
                new CilEmitTests(globalLog),
                new LocalTypeResolutionTests(),
                new MemberResolutionTests(),
                new NameConversionTests(),
                new TypeAttributeTests());
        }

        public static int FlameCompiler()
        {
            return RunTests.RunMany(
                new ArithmeticIntrinsicsTests(),
                new FlowGraphAnalysisTests(),
                new InterferenceGraphAnalysisTests(),
                new LivenessAnalysisTests(),
                new PredecessorAnalysisTests(),
                new RelatedValueAnalysisTests(),
                new ValueUseAnalysisTests());
        }

        public static int FlameIr()
        {
            return RunTests.RunMany(
                new AssemblyCodecTest(globalLog),
                new ConstantCodecTest(globalLog, globalRng),
                new PiecewiseCodecTest(globalLog),
                new TypeCodecTest(globalLog));
        }

        public static int FlameTools()
        {
            return RunTests.RunMany(
                new ILOptTests());
        }
    }
}
