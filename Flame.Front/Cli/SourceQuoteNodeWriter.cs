using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class SourceQuoteNodeWriter : INodeWriter
    {
        public SourceQuoteNodeWriter(INodeWriter MainWriter)
        {
            this.MainWriter = MainWriter;
        }

        public INodeWriter MainWriter { get; private set; }

        public static Style GetSourceQuoteStyle(IConsole Console, IStylePalette Palette)
        {
            return StyleConstants.GetBrightStyle(Palette, StyleConstants.SourceQuoteStyleName, Console.Description.ForegroundColor);
        }

        public void Write(MarkupNode Node, IConsole Console, IStylePalette Palette)
        {
            Console.PushStyle(GetSourceQuoteStyle(Console, Palette));
            NodeWriter.WriteDefault(Node, Console, Palette, MainWriter);
            Console.PopStyle();
        }
    }
}
