using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Cli
{
    public class SourceNodeWriter : INodeWriter
    {
        public SourceNodeWriter(IConsole Console, Style CaretStyle, Style HighlightStyle, string Indentation, int MaxWidth)
        {
            this.Console = Console;
            this.CaretStyle = CaretStyle;
            this.HighlightStyle = HighlightStyle;
            this.Indentation = Indentation;
            this.MaxWidth = MaxWidth;
            this.caretConsole = new IndirectConsole(Console.Description);
            this.caretConsole.PushStyle(HighlightStyle);
            this.width = 0;
        }

        public IConsole Console { get; private set; }
        public int MaxWidth { get; private set; }
        public string Indentation { get; private set; }
        public Style CaretStyle { get; private set; }
        public Style HighlightStyle { get; private set; }

        private IndirectConsole caretConsole;
        private int width;

        private void FlushLine()
        {
            if (!caretConsole.IsWhitespace)
            {
                Console.WriteLine();
                Console.Write(Indentation);
                caretConsole.PopStyle();
                caretConsole.Flush(Console);
                caretConsole.PushStyle(HighlightStyle);
            }
            else
            {
                caretConsole.Clear();
            }
            width = 0;
        }

        private void Write(IMarkupNode Node, bool UseCaret, bool CaretStarted)
        {
            if (Node.Type == NodeConstants.HighlightNodeType)
            {
                UseCaret = true;
                CaretStarted = false;
            }
            string nodeText = Node.GetText();
            foreach (var item in nodeText)
            {
                if (width == 0)
                {
                    if (char.IsWhiteSpace(item))
                    {
                        continue;
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.Write(Indentation);
                    }
                }

                width += item == '\t' ? 4 : 1;
                if (width >= MaxWidth)
                {
                    FlushLine();
                }
                if (!CaretStarted && UseCaret)
                {
                    caretConsole.Write("^", CaretStyle);
                    if (item == '\t')
                    {
                        caretConsole.Write(new string('~', 3));
                    }
                    CaretStarted = true;
                }
                else if (UseCaret)
                {
                    caretConsole.Write(item != '\t' ? "~" : new string('~', 4));
                }
                else
                {
                    caretConsole.Write(item != '\t' ? " " : new string(' ', 4));
                }
                if (item == '\t')
                {
                    Console.Write(new string(' ', 4));
                }
                else
                {
                    Console.Write(item);
                }

            }
            foreach (var item in Node.Children)
            {
                Write(item, CaretStarted, UseCaret);
            }
        }

        public void Write(IMarkupNode Node)
        {
            Console.WriteLine();
            Write(Node, false, false);
            FlushLine();
        }
    }
}
