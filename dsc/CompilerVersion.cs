using Flame.Compiler;
using Flame.Front;
using Flame.Front.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dsc
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

        public static void PrintVersion()
        {
            StringBuilder msg = new StringBuilder();
            WriteVariable("dsc's current version number is", CurrentVersion.ToString(), msg);
            WriteVariable("Platform", ConsoleEnvironment.OSVersionString, msg);
            WriteVariable("Console", ConsoleEnvironment.TerminalIdentifier, msg);
            msg.AppendLine("You can check for new releases at https://github.com/jonathanvdc/Flame/releases.");
            msg.Append("Thanks for using dsc! Have fun writing code.");
            ConsoleLog.Instance.LogMessage(new LogEntry("Current version", msg.ToString()));
        }
    }
}
