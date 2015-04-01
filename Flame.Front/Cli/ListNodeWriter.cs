using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class ListNodeWriter : INodeWriter
    {
        public ListNodeWriter(IConsole Console, INodeWriter MainWriter)
        {
            this.Console = Console;
            this.MainWriter = MainWriter;
        }

        public INodeWriter MainWriter { get; private set; }
        public IConsole Console { get; private set; }

        public void Write(IMarkupNode Node)
        {
            foreach (var item in Node.Children)
            {
                Console.WriteLine();
                Console.Write(" * ");
                MainWriter.Write(item);
            }
        }
    }
}
