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
        public ListNodeWriter(INodeWriter MainWriter)
        {
            this.MainWriter = MainWriter;
        }

        public INodeWriter MainWriter { get; private set; }

        public void Write(IMarkupNode Node, IConsole Console, IStylePalette Palette)
        {
            Console.PushStyle(Node.GetStyle(Palette));
            var itemConsole = new ListItemConsole(Console, " " + Node.Attributes.Get<string>(NodeConstants.ListBulletAttribute, "*") + " ");
            foreach (var item in Node.Children)
            {
                Console.WriteLine();
                MainWriter.Write(item, itemConsole, Palette);
                itemConsole.Reset();
            }
            Console.PopStyle();
        }
    }
}
