using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class ParagraphWriter : INodeWriter
    {
        public ParagraphWriter(INodeWriter MainWriter)
        {
            this.MainWriter = MainWriter;
        }

        public INodeWriter MainWriter { get; private set; }

        public void Write(Pixie.IMarkupNode Node, IConsole Console, IStylePalette Palette)
        {
            Console.WriteLine();
            NodeWriter.WriteDefault(Node, Console, Palette, MainWriter);
            Console.WriteLine();
        }
    }
}
