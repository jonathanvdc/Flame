using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class DefaultConsoleStyle
    {
        public DefaultConsoleStyle(Color ForegroundColor, Color BackgroundColor)
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

        public void Apply()
        {
            if (Console.BackgroundColor != BackgroundConsoleColor)
            {
                Console.BackgroundColor = BackgroundConsoleColor;
            }
            if (Console.ForegroundColor != ForegroundConsoleColor)
            {
                Console.ForegroundColor = ForegroundConsoleColor;
            }
        }
    }
}
