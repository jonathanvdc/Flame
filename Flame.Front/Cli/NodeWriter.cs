using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class NodeWriter : INodeWriter
    {
        public NodeWriter()
        {
            this.Writers = new Dictionary<string, INodeWriter>(StringComparer.OrdinalIgnoreCase);
        }

        public IDictionary<string, INodeWriter> Writers { get; private set; }

        protected virtual void WriteDefault(IMarkupNode Node, IConsole Console, IStylePalette Palette)
        {
            Console.Write(Node.GetText());
            foreach (var item in Node.Children)
            {
                Write(item, Console, Palette);
            }
        }

        public void Write(IMarkupNode Node, IConsole Console, IStylePalette Palette)
        {
            if (Writers.ContainsKey(Node.Type))
            {
                Writers[Node.Type].Write(Node, Console, Palette);
            }
            else
            {
                WriteDefault(Node, Console, Palette);
            }            
        }
    }
}
