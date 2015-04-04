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
        public HighlightingNodeWriter(INodeWriter MainWriter)
        {
            this.MainWriter = MainWriter;
        }

        public INodeWriter MainWriter { get; private set; }

        public static Style GetHighlightStyle(IStylePalette Palette)
        {
            return StyleConstants.GetDimStyle(Palette, StyleConstants.HighlightStyleName, new Color(0.0, 1.0, 1.0), "underline");
        }
        public static Style GetHighlightMissingStyle(IStylePalette Palette)
        {
            return StyleConstants.GetDimStyle(Palette, StyleConstants.HighlightMissingStyleName, new Color(1.0, 1.0, 0.0));
        }
        public static Style GetHighlightExtraStyle(IStylePalette Palette)
        {
            return StyleConstants.GetDimStyle(Palette, StyleConstants.HighlightExtraStyleName, new Color(1.0, 0.0, 1.0));
        }

        public void Write(IMarkupNode Node, IConsole Console, IStylePalette Palette)
        {
            string type = Node.Attributes.Get<string>(NodeConstants.HighlightingTypeAttribute, NodeConstants.DefaultHighlightingType);
            if (NodeConstants.MissingHighlightingType.Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                Console.PushStyle(GetHighlightMissingStyle(Palette));
            }
            else if (NodeConstants.ExtraHighlightingType.Equals(type, StringComparison.OrdinalIgnoreCase))
            {
                Console.PushStyle(GetHighlightExtraStyle(Palette));
            }
            else
            {
                Console.PushStyle(GetHighlightStyle(Palette));
            }
            Console.PushStyle(Node.GetStyle(Palette));
            Console.Write(Node.GetText());
            foreach (var item in Node.Children)
            {
                MainWriter.Write(item, Console, Palette);
            }
            Console.PopStyle();
            Console.PopStyle();
        }
    }
}
