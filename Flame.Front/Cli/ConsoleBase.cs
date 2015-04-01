using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public abstract class ConsoleBase<TStyle> : IConsole, IDisposable
    {
        public ConsoleBase(TStyle InitialStyle)
        {
            styles = new Stack<TStyle>();
            styles.Push(InitialStyle);
            this.InitialStyle = InitialStyle;
        }

        public TStyle InitialStyle { get; private set; }

        public abstract ConsoleDescription Description { get; }
        protected abstract TStyle MergeStyles(TStyle Source, Style Delta);
        protected abstract void ApplyStyle(TStyle PreviousStyle, TStyle NewStyle);
        public abstract void Dispose();

        private Stack<TStyle> styles;

        public void PushStyle(Style Value)
        {
            var peek = styles.Peek();
            var result = MergeStyles(peek, Value);
            styles.Push(result);
            ApplyStyle(peek, result);
        }

        public void PopStyle()
        {
            var old = styles.Pop();
            ApplyStyle(old, styles.Peek());
        }

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
