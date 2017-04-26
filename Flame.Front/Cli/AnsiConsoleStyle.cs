using Pixie;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class AnsiConsoleStyle
    {
        public AnsiConsoleStyle(Color ForegroundColor, Color BackgroundColor, bool Underline, bool ResetAll)
        {
            this.ForegroundColor = ForegroundColor;
            this.BackgroundColor = BackgroundColor;
            this.ForegroundConsoleColor = DefaultConsole.ToConsoleColor(ForegroundColor);
            this.BackgroundConsoleColor = DefaultConsole.ToConsoleColor(BackgroundColor);
            this.Underline = Underline;
            this.ResetAll = ResetAll;
        }

        public Color ForegroundColor { get; private set; }
        public Color BackgroundColor { get; private set; }
        public bool ResetAll { get; private set; }

        public ConsoleColor ForegroundConsoleColor { get; private set; }
        public ConsoleColor BackgroundConsoleColor { get; private set; }
        public bool Underline { get; private set; }

        public bool IsBold
        {
            get
            {
                return boldColors.Contains(ForegroundConsoleColor);
            }
        }

        public void Apply(AnsiConsoleStyle OldStyle, AnsiConsoleStyle InitialStyle)
        {
            Console.Write(GetEscapeSequence(OldStyle, InitialStyle));
        }

        private static readonly Dictionary<ConsoleColor, int> colorIdents = new Dictionary<ConsoleColor, int>()
        {
            { ConsoleColor.Black, 0 },
            { ConsoleColor.DarkRed, 1 },
            { ConsoleColor.DarkGreen, 2 },
            { ConsoleColor.DarkYellow, 3 },
            { ConsoleColor.DarkBlue, 4 },
            { ConsoleColor.DarkMagenta, 5 },
            { ConsoleColor.DarkCyan, 6 },
            { ConsoleColor.Gray, 7 },
            { ConsoleColor.DarkGray, 0 },
            { ConsoleColor.Red, 1 },
            { ConsoleColor.Green, 2 },
            { ConsoleColor.Yellow, 3 },
            { ConsoleColor.Blue, 4 },
            { ConsoleColor.Magenta, 5 },
            { ConsoleColor.Cyan, 6 },
            { ConsoleColor.White, 7 }
        };

        private static readonly HashSet<ConsoleColor> boldColors = new HashSet<ConsoleColor>()
        {
            ConsoleColor.DarkGray, ConsoleColor.Red,
            ConsoleColor.Green, ConsoleColor.Yellow,
            ConsoleColor.Blue, ConsoleColor.Magenta,
            ConsoleColor.Cyan, ConsoleColor.White
        };

        public string GetEscapeSequence(AnsiConsoleStyle OldStyle, AnsiConsoleStyle InitialStyle)
        {
            List<string> codes = new List<string>();
            bool reset = false;
            if (ResetAll || (!IsBold && OldStyle.IsBold) || (!Underline && OldStyle.Underline))
            {
                codes.Add("0");
                reset = true;
            }
            else if (IsBold && !OldStyle.IsBold)
            {
                codes.Add("1");
            }
            else if (Underline && !OldStyle.Underline)
            {
                codes.Add("4");
            }
            if (!ResetAll)
            {
                int oldFgIdent = colorIdents[OldStyle.ForegroundConsoleColor];
                int oldBgIdent = colorIdents[OldStyle.BackgroundConsoleColor];
                int newFgIdent = colorIdents[ForegroundConsoleColor];
                int newBgIdent = colorIdents[BackgroundConsoleColor];
                if (reset && (newFgIdent != colorIdents[InitialStyle.ForegroundConsoleColor]) || newFgIdent != oldFgIdent)
                {
                    codes.Add("3" + newFgIdent.ToString(CultureInfo.InvariantCulture));
                }
                if (reset && (newBgIdent != colorIdents[InitialStyle.BackgroundConsoleColor]) || newBgIdent != oldBgIdent)
                {
                    codes.Add("4" + newBgIdent.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (codes.Count == 0)
            {
                return "";
            }
            else
            {
                return "\x1b[" + string.Join(";", codes) + "m";
            }
        }
    }
}
