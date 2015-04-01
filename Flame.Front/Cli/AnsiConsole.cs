using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class AnsiConsole : ConsoleBase<AnsiConsoleStyle>
    {
        public AnsiConsole(string Name, int BufferWidth, Color ForegroundColor, Color BackgroundColor)
            : base(new AnsiConsoleStyle(ForegroundColor.Over(DefaultForegroundColor), BackgroundColor.Over(DefaultBackgroundColor), true))
        {
            desc = new ConsoleDescription("default", BufferWidth, 
                InitialStyle.ForegroundColor, InitialStyle.BackgroundColor);
        }
        public AnsiConsole(string Name, int BufferWidth)
            : this(Name, BufferWidth, new Color(), new Color())
        {
        }

        public static Color DefaultBackgroundColor
        {
            get
            {
                return DefaultConsole.DefaultBackgroundColor;
            }
        }

        public static Color DefaultForegroundColor
        {
            get
            {
                return DefaultConsole.DefaultForegroundColor;
            }
        }

        private ConsoleDescription desc;

        public override ConsoleDescription Description
        {
            get { return desc; }
        }

        protected override AnsiConsoleStyle MergeStyles(AnsiConsoleStyle Source, Style Delta)
        {
            return new AnsiConsoleStyle(Delta.ForegroundColor.Over(Source.ForegroundColor), Delta.BackgroundColor.Over(Source.BackgroundColor), Source == null);
        }

        protected override void ApplyStyle(AnsiConsoleStyle OldStyle, AnsiConsoleStyle Style)
        {
            Style.Apply(OldStyle, InitialStyle);
        }

        public override void Dispose()
        {
            Console.Write("\x1b[0m");
        }
    }
}
