using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class IndirectConsole : IConsole
    {
        public IndirectConsole(ConsoleDescription Description)
        {
            this.Description = Description;
            this.commands = new List<Action<IConsole>>();
            this.IsWhitespace = true;
        }

        public ConsoleDescription Description { get; private set; }
        public bool IsWhitespace { get; private set; }

        public void Flush(IConsole Console)
        {
            foreach (var item in commands)
            {
                item(Console);
            }
            Clear();
        }

        public void Clear()
        {
            commands.Clear();
            IsWhitespace = true;
        }

        private List<Action<IConsole>> commands;

        public void PushStyle(Style Value)
        {
            commands.Add(target => target.PushStyle(Value));
        }

        public void PopStyle()
        {
            commands.Add(target => target.PopStyle());
        }

        public void Write(string Text)
        {
            if (!string.IsNullOrWhiteSpace(Text))
            {
                IsWhitespace = false;
            }
            commands.Add(target => target.Write(Text));
        }

        public void WriteLine()
        {
            commands.Add(target => target.WriteLine());
        }

        public void Dispose()
        {
            commands.Add(target => target.Dispose());
        }
    }
}
