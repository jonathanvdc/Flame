using System;
using Flame.Front.Cli;
using Flame.Front.Target;

namespace Flame.Wasm
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            BuildTargetParsers.Parser.RegisterParser(new WasmBuildTargetParser());
            var compiler = new ConsoleCompiler(
                "flame-wasm",
                "the Flame IR -> WebAssembly compiler",
                "https://github.com/jonathanvdc/Flame/releases");
            Environment.Exit(compiler.Compile(args));
        }
    }
}
