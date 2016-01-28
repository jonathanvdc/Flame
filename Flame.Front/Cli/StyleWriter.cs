using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    /// <summary>
    /// A type of node writer that prints a node's 
    /// contents in a specific style. 
    /// </summary>
    public class StyleWriter : INodeWriter
    {
        public StyleWriter(INodeWriter MainWriter, Style NodeStyle)
        {
            this.MainWriter = MainWriter;
            this.NodeStyle = NodeStyle;
        }

        public INodeWriter MainWriter { get; private set; }
        public Style NodeStyle { get; private set; }

        public void Write(MarkupNode Node, IConsole Console, IStylePalette Palette)
        {
            Console.PushStyle(Node.GetStyle(Palette));
            Console.PushStyle(NodeStyle);
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
