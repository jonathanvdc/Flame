using System;
using System.Collections.Generic;
using Loyc;
using Loyc.Collections;
using Loyc.MiniTest;
using System.Diagnostics;
using Loyc.Syntax;

namespace UnitTests
{
    // Test driver based on Loyc project: https://github.com/qwertie/ecsharp/blob/master/Core/Tests/Program.cs

    public class Program
    {
        public static readonly VList<Pair<string, Func<int>>> Menu = new VList<Pair<string, Func<int>>>()
        {
            new Pair<string,Func<int>>("Run unit tests of Flame.dll",  Flame),
            new Pair<string,Func<int>>("Run unit tests of Flame.Build.Lazy.dll",  Flame_Build_Lazy),
            new Pair<string,Func<int>>("Run unit tests of Flame.Compiler.dll",  Flame_Compiler),
            new Pair<string,Func<int>>("Run unit tests of Flame.DSharp.dll",  Flame_DSharp),
            new Pair<string,Func<int>>("Run unit tests of Flame.Optimization.dll",  Flame_Optimization)
        };

        public static void Main(string[] args)
        {
            // Workaround for MS bug: Assert(false) will not fire in debugger
            Debug.Listeners.Clear();
            Debug.Listeners.Add( new DefaultTraceListener() );
            if (RunMenu(Menu, args.Length > 0 ? args[0].GetEnumerator() : null) > 0)
                // Let the outside world know that something went wrong (e.g. Travis CI)
                Environment.ExitCode = 1;
        }

        private static IEnumerator<char> ConsoleChars()
        {
            for (ConsoleKeyInfo k; (k = Console.ReadKey(true)).Key != ConsoleKey.Escape
                && k.Key != ConsoleKey.Enter;)
                yield return k.KeyChar;
        }

        public static int RunMenu(IList<Pair<string, Func<int>>> menu, IEnumerator<char> input = null)
        {
            var reader = input ?? ConsoleChars();
            int errorCount = 0;
            for (;;) {
                Console.WriteLine();
                Console.WriteLine("What do you want to do? (Esc to quit)");
                for (int i = 0; i < menu.Count; i++)
                    Console.WriteLine(PrintHelpers.HexDigitChar(i+1) + ". " + menu[i].Key);
                Console.WriteLine("Space. Run all tests");

                if (!reader.MoveNext())
                    break;

                char c = reader.Current;
                if (c == ' ') {
                    for (int i = 0; i < menu.Count; i++) {
                        Console.WriteLine();
                        ConsoleMessageSink.WriteColoredMessage(ConsoleColor.White, i+1, menu[i].Key);
                        errorCount += menu[i].Value();
                    }
                } else {
                    int i = ParseHelpers.HexDigitValue(c);
                    if (i > 0 && i <= menu.Count)
                        errorCount += menu[i - 1].Value();
                }
            }
            return errorCount;
        }

        public static int Flame()
        {
            return RunTests.RunMany(
                new SetTests(),
                new ShadowingTests(),
                new TypeSystemTests(),
                new IntegerValueTests(),
                new QualifiedNameTests(),
                new AttributeMapTests(),
                new Collections.ValueListTests(),
                new Collections.SmallMultiDictionaryTests());
        }

        public static int Flame_Build_Lazy()
        {
            return RunTests.RunMany(
                new Build.Lazy.DeferredInitializerTests());
        }

        public static int Flame_Compiler()
        {
            return RunTests.RunMany(
                new Compiler.FlowGraphTests(),
                new Compiler.LocationFinderTests());
        }

        public static int Flame_DSharp()
        {
            return RunTests.RunMany(
                new DSharp.LexerTests(),
                new DSharp.ParserTests(),
                new DSharp.SemanticsTests());
        }

        public static int Flame_Optimization()
        {
            return RunTests.RunMany(
                new Optimization.SimplifyFlowTests());
        }
    }
}
