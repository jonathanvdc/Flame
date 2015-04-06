using Flame.Compiler;
using Flame.Front;
using Flame.Front.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public static class CompilerVersion
    {
        public static Version CurrentVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        public static void WriteVariable(string Name, string Value, StringBuilder Target)
        {
            if (!string.IsNullOrWhiteSpace(Value))
            {
                Target.Append(Name);
                Target.Append(": '");
                Target.Append(Value);
                Target.AppendLine("'.");
            }
        }

        public static void PrintVersion(string CompilerName, string CompilerReleasesSite, ICompilerLog Log)
        {
            StringBuilder msg = new StringBuilder();
            WriteVariable(CompilerName + "'s current version number is", CurrentVersion.ToString(), msg);
            WriteVariable("Platform", ConsoleEnvironment.OSVersionString, msg);
            WriteVariable("Console", ConsoleEnvironment.TerminalIdentifier, msg);
            msg.AppendLine("You can check for new releases at " + CompilerReleasesSite + ".");
            msg.Append("Thanks for using " + CompilerName + "! Have fun writing code.");
            Log.LogMessage(new LogEntry("Current version", msg.ToString()));
        }
    }
}
