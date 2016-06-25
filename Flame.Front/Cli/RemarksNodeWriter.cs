using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class RemarksNodeWriter : INodeWriter
    {
        public RemarksNodeWriter(INodeWriter MainWriter)
        {
            this.MainWriter = MainWriter;
        }

        public INodeWriter MainWriter { get; private set; }

        public static Style GetRemarksStyle(IStylePalette Palette)
        {
            return StyleConstants.GetDimStyle(Palette, StyleConstants.RemarksStyleName, new Color(0.75));
        }

        public void Write(MarkupNode Node, IConsole Console, IStylePalette Palette)
        {
            Console.WriteLine();
            Console.PushStyle(Node.GetStyle(Palette));

            int prefixCount = Node.Attributes.Get<int>(
                "remark-prefix-count", 0);

            foreach (var item in Node.Children.Take(prefixCount))
            {
                MainWriter.Write(item, Console, Palette);
            }

            Console.PushStyle(GetRemarksStyle(Palette));
            Console.Write("remark: ");
            Console.PopStyle();

            Console.Write(Node.GetText());
            foreach (var item in Node.Children.Skip(prefixCount))
            {
                MainWriter.Write(item, Console, Palette);
            }
            Console.PopStyle();
        }
    }
}
