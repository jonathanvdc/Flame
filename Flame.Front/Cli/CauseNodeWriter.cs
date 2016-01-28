using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pixie;

namespace Flame.Front.Cli
{
    public class CauseNodeWriter : INodeWriter
    {
        public CauseNodeWriter(INodeWriter MainWriter)
        {
            this.MainWriter = MainWriter;
        }

        public INodeWriter MainWriter { get; private set; }

        public void Write(MarkupNode Node, IConsole Console, IStylePalette Palette)
        {
            Console.PushStyle(Node.GetStyle(Palette));
            Console.Write("[-");
            Console.Write(Node.GetText());
            foreach (var item in Node.Children)
            {
                MainWriter.Write(item, Console, Palette);
            }
            Console.Write("]");
            Console.PopStyle();
        }
    }
}
