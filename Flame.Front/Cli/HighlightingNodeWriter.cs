using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class HighlightingNodeWriter : INodeWriter
    {
        public HighlightingNodeWriter(IConsole Console, INodeWriter MainWriter, Style HighlightStyle, Style HighlightMissingStyle, Style HighlightExtraStyle)
        {
            this.Console = Console;
            this.MainWriter = MainWriter;
            this.HighlightStyle = HighlightStyle;
            this.HighlightMissingStyle = HighlightMissingStyle;
            this.HighlightExtraStyle = HighlightExtraStyle;
        }

        public INodeWriter MainWriter { get; private set; }
        public IConsole Console { get; private set; }
        public Style HighlightStyle { get; private set; }
        public Style HighlightMissingStyle { get; private set; }
        public Style HighlightExtraStyle { get; private set; }

        public void Write(IMarkupNode Node)
        {
            string type = Node.Attributes.Get<string>("type", "default");
            if ("missing".Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                Console.PushStyle(HighlightMissingStyle);
            }
            else if ("extra".Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                Console.PushStyle(HighlightExtraStyle);
            }
            else
            {
                Console.PushStyle(HighlightStyle);
            }
            Console.Write(Node.GetText());
            foreach (var item in Node.Children)
            {
                MainWriter.Write(item);
            }
            Console.PopStyle();
        }
    }
}
