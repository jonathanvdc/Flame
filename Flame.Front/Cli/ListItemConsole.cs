using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class ListItemConsole : IConsole
    {
        public ListItemConsole(IConsole Console, string Bullet)
        {
            this.Console = Console;
            this.Bullet = Bullet;
            this.Reset();
        }

        public string Bullet { get; private set; }
        public string Indentation { get { return new string(' ', Bullet.Length); } }
        public IConsole Console { get; private set; }

        private bool onNewline;
        private bool onFirstLine;

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
            if (!string.IsNullOrEmpty(Text))
            {
                lock (Console)
                {
                    if (onFirstLine)
                    {
                        Console.Write(Bullet);
                        onFirstLine = false;
                        onNewline = false;
                    }
                    else if (onNewline)
                    {
                        Console.Write(Indentation);
                        onNewline = false;
                    }
                    Console.Write(Text);
                }
            }
        }

        public void WriteLine()
        {
            lock (Console)
            {
                Console.WriteLine();
                onNewline = true;
            }
        }

        public void Reset()
        {
            lock (Console)
            {
                this.onFirstLine = true;
                this.onNewline = true;
            }
        }

        public void Dispose()
        {
            Console.Dispose();
        }
    }
}
