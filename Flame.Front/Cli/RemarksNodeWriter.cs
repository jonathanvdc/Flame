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
        public RemarksNodeWriter(IConsole Console, INodeWriter MainWriter, Style RemarksStyle)
        {
            this.Console = Console;
            this.MainWriter = MainWriter;
            this.RemarksStyle = RemarksStyle;
        }

        public INodeWriter MainWriter { get; private set; }
        public IConsole Console { get; private set; }
        public Style RemarksStyle { get; private set; }

        public void Write(IMarkupNode Node)
        {
            Console.WriteLine();
            Console.PushStyle(RemarksStyle);
            Console.Write("Remarks: ");
            Console.Write(Node.GetText());
            foreach (var item in Node.Children)
            {
                MainWriter.Write(item);
            }
            Console.PopStyle();
        }
    }
}
