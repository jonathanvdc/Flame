using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class ParagraphConsole : IConsole
    {
        public ParagraphConsole(IConsole Console)
            : this(Console, 0)
        {
        }
        public ParagraphConsole(IConsole Console, int NewlineCount)
        {
            this.Console = Console;
            this.newlineCount = NewlineCount;
        }

        public IConsole Console { get; private set; }

        private int newlineCount;

        public ConsoleDescription Description
        {
            get { return Console.Description; }
        }

        public void PushStyle(Style Value)
        {
            Console.PushStyle(Value);
        }

        public void PopStyle()
        {
            Console.PopStyle();
        }

        public void Write(string Text)
        {
            lock (Console)
            {
                Console.Write(Text);
                newlineCount = 0;
            }
        }

        public void WriteLine()
        {
            lock (Console)
            {
                Console.WriteLine();
                newlineCount++;
            }
        }

        public void WriteSeparator(int Size)
        {
            lock (Console)
            {
                if (Size > newlineCount)
                {
                    while (newlineCount < Size)
                    {
                        Console.WriteLine();
                        newlineCount++;
                    }
                }
            }
        }

        public void Dispose()
        {
            Console.Dispose();
        }
    }
}
