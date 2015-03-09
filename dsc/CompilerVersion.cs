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
            ConsoleLog.Instance.LogMessage(new LogEntry("Current version", "dsc's current version number is " + CurrentVersion + "."));
        }
    }
}
