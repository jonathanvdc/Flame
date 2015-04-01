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
        public NodeWriter(IConsole Console)
        {
            this.Console = Console;
            this.Writers = new Dictionary<string, INodeWriter>(StringComparer.OrdinalIgnoreCase);
        }

        public IConsole Console { get; private set; }
        public IDictionary<string, INodeWriter> Writers { get; private set; }

        protected virtual void WriteDefault(IMarkupNode Node)
        {
            Console.Write(Node.GetText());
            foreach (var item in Node.Children)
            {
                Write(item);
            }
        }

        public void Write(IMarkupNode Node)
        {
            if (Writers.ContainsKey(Node.Type))
            {
                Writers[Node.Type].Write(Node);
            }
            else
            {
                WriteDefault(Node);
            }            
        }
    }
}
