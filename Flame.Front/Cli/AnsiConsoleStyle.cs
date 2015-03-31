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
        public AnsiConsoleStyle(AnsiConsoleStyle PreviousStyle, Color ForegroundColor, Color BackgroundColor)
        {
            this.ForegroundColor = ForegroundColor;
            this.BackgroundColor = BackgroundColor;
            this.ForegroundConsoleColor = DefaultConsole.ToConsoleColor(ForegroundColor);
            this.BackgroundConsoleColor = DefaultConsole.ToConsoleColor(BackgroundColor);
        }

        public Color ForegroundColor { get; private set; }
        public Color BackgroundColor { get; private set; }

        public ConsoleColor ForegroundConsoleColor { get; private set; }
        public ConsoleColor BackgroundConsoleColor { get; private set; }

        public bool IsBold
        {
            get
            {
                return boldColors.Contains(ForegroundConsoleColor);
            }
        }

        public void Apply()
        {
            Console.Write(GetEscapeSequence());
        }

        static AnsiConsoleStyle()
        {
            colorIdents = new Dictionary<ConsoleColor, int>();
            colorIdents[ConsoleColor.Black] = 0;
            colorIdents[ConsoleColor.DarkRed] = 1;
            colorIdents[ConsoleColor.DarkGreen] = 2;
            colorIdents[ConsoleColor.DarkYellow] = 3;
            colorIdents[ConsoleColor.DarkBlue] = 4;
            colorIdents[ConsoleColor.DarkMagenta] = 5;
            colorIdents[ConsoleColor.DarkCyan] = 6;
            colorIdents[ConsoleColor.Gray] = 7;
            colorIdents[ConsoleColor.DarkGray] = 0;
            colorIdents[ConsoleColor.Red] = 1;
            colorIdents[ConsoleColor.Green] = 2;
            colorIdents[ConsoleColor.Yellow] = 3;
            colorIdents[ConsoleColor.Blue] = 4;
            colorIdents[ConsoleColor.Magenta] = 5;
            colorIdents[ConsoleColor.Cyan] = 6;
            colorIdents[ConsoleColor.White] = 7;

            boldColors = new HashSet<ConsoleColor>();
            boldColors.Add(ConsoleColor.DarkGray);
            boldColors.Add(ConsoleColor.Red);
            boldColors.Add(ConsoleColor.Green);
            boldColors.Add(ConsoleColor.Yellow);
            boldColors.Add(ConsoleColor.Blue);
            boldColors.Add(ConsoleColor.Magenta);
            boldColors.Add(ConsoleColor.Cyan);
            boldColors.Add(ConsoleColor.White);
        }

        private static Dictionary<ConsoleColor, int> colorIdents;
        private static HashSet<ConsoleColor> boldColors;

        public string GetEscapeSequence()
        {
            List<string> codes = new List<string>();
            if (IsBold)
            {
                codes.Add("1");
            }
            else
            {
                codes.Add("0");
            }
            codes.Add("3" + colorIdents[ForegroundConsoleColor].ToString(CultureInfo.InvariantCulture));
            codes.Add("4" + colorIdents[BackgroundConsoleColor].ToString(CultureInfo.InvariantCulture));

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
