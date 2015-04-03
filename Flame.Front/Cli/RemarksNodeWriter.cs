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

        public void Write(IMarkupNode Node, IConsole Console, IStylePalette Palette)
        {
            Console.WriteLine();
            Console.PushStyle(GetRemarksStyle(Palette));
            Console.PushStyle(Node.GetStyle(Palette));
            Console.Write("Remarks: ");
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
