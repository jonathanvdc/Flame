using Flame.Compiler;
using Flame.Front.Options;
using Pixie;
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
        /// The vt220 terminal identifier.
        /// </summary>
        public const string VT220Identifier = "vt220";
        /// <summary>
        /// The default linux terminal identifier.
        /// </summary>
        public const string LinuxIdentifier = "linux";
        /// <summary>
        /// The html output terminal identifier.
        /// </summary>
        public const string HtmlIdentifier = "html";

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

        public static bool IsXTerminalIdentifier(string Identifier)
        {
            return Identifier != null && (Identifier.Equals(XTermIdentifier, StringComparison.OrdinalIgnoreCase) || Identifier.StartsWith(XTermIdentifier + "-", StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsVTerminal(string Identifier)
        {
            return Identifier != null && Identifier.StartsWith("vt", StringComparison.OrdinalIgnoreCase) && Identifier.Substring(2).All(char.IsDigit);
        }

        static ConsoleEnvironment()
        {
            registeredConsoles = new List<KeyValuePair<Func<string, bool>, Func<string, ICompilerOptions, IConsole>>>();
            RegisterConsole(name => IsXTerminalIdentifier(name) || IsVTerminal(name) || name.Equals(LinuxIdentifier, StringComparison.OrdinalIgnoreCase),
                (name, ops) => new AnsiConsole(name, DefaultConsole.GetBufferWidth(), GetForegroundColor(ops), GetBackgroundColor(ops)));
            RegisterConsole(name => name != null && name.Equals(HtmlIdentifier, StringComparison.OrdinalIgnoreCase),
                (name, ops) => new HtmlConsole(new ConsoleDescription(name, 80, GetForegroundColor(ops), GetBackgroundColor(ops)), OverridesDefaultStyle(ops), EmbedHtmlStyle(ops), IdentHtml(ops)));
        }

        private static List<KeyValuePair<Func<string, bool>, Func<string, ICompilerOptions, IConsole>>> registeredConsoles;

        public static void RegisterConsole(Func<string, bool> Predicate, Func<string, ICompilerOptions, IConsole> Builder)
        {
            registeredConsoles.Add(new KeyValuePair<Func<string, bool>, Func<string, ICompilerOptions, IConsole>>(Predicate, Builder));
        }

        private static Color GetForegroundColor(ICompilerOptions Options)
        {
            return Options.GetOption<Color>("fg-color", new Color());
        }
        private static Color GetBackgroundColor(ICompilerOptions Options)
        {
            return Options.GetOption<Color>("bg-color", new Color());
        }
        private static bool OverridesDefaultStyle(ICompilerOptions Options)
        {
            return Options.GetOption<bool>("override-style", false);
        }
        private static bool EmbedHtmlStyle(ICompilerOptions Options)
        {
            return Options.GetOption<bool>("embed-html-style", false);
        }
        private static bool IdentHtml(ICompilerOptions Options)
        {
            return Options.GetOption<bool>("indent-html", true);
        }

        public static IConsole AcquireConsole(string Identifier, ICompilerOptions Options)
        {
            foreach (var item in registeredConsoles)
            {
                if (item.Key(Identifier))
                {
                    return item.Value(Identifier, Options);
                }
            }
            return new DefaultConsole(DefaultConsole.GetBufferWidth(), GetForegroundColor(Options), GetBackgroundColor(Options));
        }
        public static IConsole AcquireConsole(ICompilerOptions Options)
        {
            return AcquireConsole(Options.GetOption<string>("terminal", TerminalIdentifier), Options);
        }
        public static IConsole AcquireConsole()
        {
            return AcquireConsole(TerminalIdentifier, new StringCompilerOptions());
        }
    }
}
