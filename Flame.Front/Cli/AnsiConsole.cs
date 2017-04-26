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
            : base(new AnsiConsoleStyle(ForegroundColor.Over(DefaultForegroundColor), BackgroundColor.Over(DefaultBackgroundColor), false, true))
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
                // Mono seems to default to black as background color,
                // no matter what the terminal color is actually set to.
                // This makes asking the Console class just about 
                // useless. Let's save us all some trouble, and assume
                // that the background color is indeed black for ANSI
                // escape code consoles.
                return new Color(0, 0, 0, 1);
            }
        }

        public static Color DefaultForegroundColor
        {
            get
            {
                // Mono seems to default to white as background color,
                // no matter the settings. Let's just assume that the
                // foreground color is gray instead, because that
                // color scheme lets us print far more interesting 
                // diagnostics.
                return new Color(0.75, 0.75, 0.75, 1);
            }
        }

        private ConsoleDescription desc;

        public override ConsoleDescription Description
        {
            get { return desc; }
        }

        protected override AnsiConsoleStyle MergeStyles(AnsiConsoleStyle Source, Style Delta)
        {
            var result = new AnsiConsoleStyle(
                Delta.ForegroundColor.Over(Source.ForegroundColor),
                Delta.BackgroundColor.Over(Source.BackgroundColor),
                Delta.Preferences.Contains("underline", StringComparer.OrdinalIgnoreCase),
                Source == null);

            if (result.ForegroundConsoleColor == InitialStyle.ForegroundConsoleColor
                && result.BackgroundConsoleColor == InitialStyle.BackgroundConsoleColor)
            {
                return new AnsiConsoleStyle(
                    result.ForegroundColor,
                    result.BackgroundColor,
                    result.Underline,
                    true);
            }
            else
            {
                return result;
            }
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
