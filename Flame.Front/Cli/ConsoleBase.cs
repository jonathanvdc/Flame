using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public abstract class ConsoleBase : IConsole
    {
        public abstract ConsoleDescription Description { get; }

        public void Write(string Text)
        {
            System.Console.Write(Text);
        }

        public void WriteLine()
        {
            System.Console.WriteLine();
        }
    }
}
