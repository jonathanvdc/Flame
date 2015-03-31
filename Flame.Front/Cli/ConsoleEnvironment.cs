using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public static class ConsoleEnvironment
    {
        /// <summary>
        /// The environment variable name for the terminal identifier.
        /// </summary>
        public const string TerminalVariableName = "TERM";
        /// <summary>
        /// The xterm terminal identifier.
        /// </summary>
        public const string XTermIdentifier = "xterm";
        /// <summary>
        /// The vt100 terminal identifier.
        /// </summary>
        public const string VT100Identifier = "vt100";
        /// <summary>
        /// The vt102 terminal identifier.
        /// </summary>
        public const string VT102Identifier = "vt102";
        /// <summary>
        /// The default linux terminal identifier.
        /// </summary>
        public const string LinuxIdentifier = "linux";

        public static string OSVersionString
        {
            get
            {
                return Environment.OSVersion.VersionString;
            }
        }

        public static string TerminalIdentifier
        {
            get
            {
                return Environment.GetEnvironmentVariable(TerminalVariableName);
            }
        }

        static ConsoleEnvironment()
        {
            registeredConsoles = new List<KeyValuePair<Func<string, bool>, Func<string, IConsole>>>();
            RegisterConsole(name => name != null && name.Equals(XTermIdentifier, StringComparison.OrdinalIgnoreCase), name => new AnsiConsole(name, 80));
        }

        private static List<KeyValuePair<Func<string, bool>, Func<string, IConsole>>> registeredConsoles;

        public static void RegisterConsole(Func<string, bool> Predicate, Func<string, IConsole> Builder)
        {
            registeredConsoles.Add(new KeyValuePair<Func<string, bool>, Func<string, IConsole>>(Predicate, Builder));
        }

        public static IConsole AcquireConsole(string Identifier)
        {
            foreach (var item in registeredConsoles)
            {
                if (item.Key(Identifier))
                {
                    return item.Value(Identifier);
                }
            }
            return new DefaultConsole(DefaultConsole.GetBufferWidth());
        }
        public static IConsole AcquireConsole()
        {
            return AcquireConsole(TerminalIdentifier);
        }
    }
}
