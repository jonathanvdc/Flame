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
        public AnsiConsole(string Name, int BufferWidth)
        {
            var initStyle = GetInitialStyle();
            this.desc = new ConsoleDescription(Name, BufferWidth, initStyle.ForegroundColor, initStyle.BackgroundColor);
        }

        private ConsoleDescription desc;

        public override ConsoleDescription Description
        {
            get { return desc; }
        }

        protected override AnsiConsoleStyle GetInitialStyle()
        {
            return new AnsiConsoleStyle(null, new Color(0.75), new Color(0.0));
        }

        protected override AnsiConsoleStyle MergeStyles(AnsiConsoleStyle Source, Style Delta)
        {
            return new AnsiConsoleStyle(Source, Delta.ForegroundColor, Delta.BackgroundColor);
        }

        protected override void ApplyStyle(AnsiConsoleStyle Style)
        {
            Style.Apply();
        }

        public override void Dispose()
        {
            Console.Write("\x1b[0m");
        }
    }
}
