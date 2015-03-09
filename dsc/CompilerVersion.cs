using Flame.Compiler;
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

        public static void PrintVersion()
        {
            StringBuilder msg = new StringBuilder();
            msg.Append("dsc's current version number is '");
            msg.Append(CurrentVersion);
            msg.AppendLine("'.");
            msg.AppendLine("You can check for new releases at https://github.com/jonathanvdc/Flame/releases.");
            msg.Append("Thanks for using dsc! Have fun writing code.");
            ConsoleLog.Instance.LogMessage(new LogEntry("Current version", msg.ToString()));
        }
    }
}
