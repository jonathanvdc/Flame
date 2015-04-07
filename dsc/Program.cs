using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Compiler.Variables;
using Flame.DSharp.Build;
using Flame.DSharp.Lexer;
using Flame.DSharp.Parser;
using Flame.DSProject;
using Flame.Recompilation;
using Flame.Verification;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Front.Projects;
using Flame.Front.Target;
using Flame.Front.Options;
using Flame.Front.State;
using Flame.Front;
using Flame.Front.Cli;
using dsc.Projects;

namespace dsc
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            ProjectHandlers.RegisterHandler(new DSharpProjectHandler());
            var compiler = new ConsoleCompiler("dsc", "the glorious D# compiler", "https://github.com/jonathanvdc/Flame/releases");
            compiler.Compile(args);
        }
    }
}
