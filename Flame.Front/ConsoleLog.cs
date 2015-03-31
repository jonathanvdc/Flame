using Flame.Compiler;
using Flame.Front.Cli;
using Flame.Front.Options;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public sealed class ConsoleLog : ICompilerLog
    {
        public ConsoleLog(IConsole Console, ICompilerOptions Options)
        {
            this.Options = Options;
            this.Console = Console;
            this.gapQueued = false;
            this.writeLock = new object();
        }

        public IConsole Console { get; private set; }
        public ICompilerOptions Options { get; private set; }
        private bool gapQueued;
        private object writeLock;

        public int BufferWidth
        {
            get
            {
                return Console.Description.BufferWidth;
            }
        }

        private void WriteGap()
        {
            lock (writeLock)
            {
                if (gapQueued)
                {
                    Console.WriteLine();
                    gapQueued = false;
                }
            }
        }

        private void WriteInternal(string Text)
        {
            Console.Write(Text);
        }
        private void WriteInternal(char Value)
        {
            Console.Write(Value);
        }
        private void WriteLineInternal(string Text)
        {
            Console.WriteLine(Text);
        }
        private void WriteLineInternal()
        {
            Console.WriteLine();
        }
        private void WriteUnsafe(string Text)
        {
            WriteGap();
            WriteInternal(Text);
        }
        private void WriteUnsafe(char Value)
        {
            WriteGap();
            WriteInternal(Value);
        }

        public void WriteWhiteline()
        {
            lock (writeLock)
            {
                gapQueued = true;
            }
        }
        public void WriteSeparator()
        {
            lock (writeLock)
            {
                WriteLineInternal();
                if (!gapQueued)
                {
                    WriteWhiteline();
                }
            }
        }
        public void Write(string Text, Color Color)
        {
            lock (writeLock)
            {
                WriteGap();
                Console.PushStyle(new Style("custom", Color, new Color()));
                WriteInternal(Text);
                Console.PopStyle();
            }
        }
        public void Write(string Text)
        {
            lock (writeLock)
            {
                WriteUnsafe(Text);
            }
        }
        public void Write(char Value)
        {
            lock (writeLock)
            {
                WriteUnsafe(Value);
            }
        }
        public void Write<T>(T Value)
        {
            Write(Value.ToString());
        }
        public void WriteLine(string Text, Color Color)
        {
            lock (writeLock)
            {
                WriteGap();
                Console.PushStyle(new Style("custom", Color, new Color()));
                WriteLineInternal(Text);
                Console.PopStyle();
            }
        }
        public void WriteLine()
        {
            WriteLine("");
        }
        public void WriteLine(string Text)
        {
            lock (writeLock)
            {
                WriteGap();
                WriteLineInternal(Text);
            }
        }
        public void WriteLine<T>(T Value)
        {
            WriteLine(Value.ToString());
        }
        public void WriteBlockEntry(string Header, Color MainColor, Color HighlightColor, LogEntry Entry)
        {
            lock (writeLock)
            {
                WriteWhiteline();
                Write(Header + ": ", MainColor);
                WriteEntry(Entry, MainColor, HighlightColor);
                WriteSeparator();
            }
        }
        public void WriteBlockEntry(string Header, Color HeaderColor, string Entry)
        {
            lock (writeLock)
            {
                WriteWhiteline();
                Write(Header + ": ", HeaderColor);
                WriteLine(Entry);
                WriteSeparator();
            }
        }
        public void WriteBlockEntry(string Header, LogEntry Entry)
        {
            lock (writeLock)
            {
                WriteWhiteline();
                Write(Header + ": ");
                WriteEntry(Entry, DefaultConsole.ToPixieColor(ConsoleColor.Green), DefaultConsole.ToPixieColor(ConsoleColor.DarkGreen));
                WriteSeparator();
            }
        }
        public void WriteBlockEntry(LogEntry Entry)
        {
            lock (writeLock)
            {
                WriteWhiteline();
                WriteEntry(Entry, DefaultConsole.ToPixieColor(ConsoleColor.Green), DefaultConsole.ToPixieColor(ConsoleColor.DarkGreen));
                WriteSeparator();
            }
        }
        public void WriteBlockEntry(string Header, string Entry)
        {
            lock (writeLock)
            {
                WriteWhiteline();
                Write(Header + ": ");
                WriteLine(Entry);
                WriteSeparator();
            }
        }
        public void WriteErrorBlock(string Header, string Message)
        {
            WriteBlockEntry(Header, DefaultConsole.ToPixieColor(ConsoleColor.Red), Message);
        }

        #region Write*Node

        public void WriteNode(IMarkupNode Node, Color CaretColor, Color HighlightColor)
        {
            lock (writeLock)
            {
                WriteNodeCore(Node, CaretColor, HighlightColor);
            }
        }

        private void WriteNodeDefault(IMarkupNode Node, Color CaretColor, Color HighlightColor)
        {
            Write(Node.GetText());
            foreach (var item in Node.Children)
            {
                WriteNodeCore(item, CaretColor, HighlightColor);
            }
        }

        private void WriteNodeCore(IMarkupNode Node, Color CaretColor, Color HighlightColor)
        {
            if (Node.Type == NodeConstants.SourceNodeType)
            {
                WriteWhiteline();
                WriteSourceNode(Node, CaretColor, HighlightColor);
                WriteWhiteline();
            }
            else if (Node.Type == NodeConstants.RemarksNodeType)
            {
                WriteWhiteline();
                Console.PushStyle(new Style("remarks", new Color(0.3), new Color()));
                Write("Remarks: ");
                WriteNodeDefault(Node, CaretColor, HighlightColor);
                Console.PopStyle();
                WriteWhiteline();
            }
            else
            {
                WriteNodeDefault(Node, CaretColor, HighlightColor);
            }
        }

        #region WriteSourceNode

        private void WriteSourceNode(IMarkupNode Node, Color CaretColor, Color HighlightColor)
        {
            int width = 0;
            var caret = new IndirectConsole(Console.Description);
            caret.PushStyle(new Style("caret-highlight", HighlightColor, new Color()));
            string indent = new string(' ', 4);
            int bufWidth = BufferWidth - indent.Length - 4;
            WriteWhiteline();
            WriteLine();
            Write(indent);
            WriteSourceNode(Node, false, false, caret, indent, bufWidth, CaretColor, HighlightColor, ref width);
            if (width > 0)
            {
                Console.WriteLine();
                Write(indent);
                caret.PopStyle();
                caret.Flush(Console);
            }
        }

        private void WriteSourceNode(IMarkupNode Node, bool CaretStarted, bool UseCaret, IndirectConsole CaretConsole,
            string Indentation, int MaxWidth, Color CaretColor, Color HighlightColor, ref int Width)
        {
            if (Node.Type.Equals(NodeConstants.HighlightNodeType))
            {
                UseCaret = true;
                CaretStarted = false;
            }
            string nodeText = Node.GetText();
            foreach (var item in nodeText)
            {
                if (Width == 0)
                {
                    if (char.IsWhiteSpace(item))
                    {
                        continue;
                    }
                }

                Width += item == '\t' ? 4 : 1;
                if (Width >= MaxWidth)
                {
                    WriteLine();
                    Write(Indentation);
                    if (!CaretConsole.IsWhitespace)
                    {
                        WriteLine();
                        Write(Indentation);
                        CaretConsole.PopStyle();
                        CaretConsole.Flush(Console);
                        CaretConsole.PushStyle(new Style("caret-highlight", HighlightColor, new Color()));
                    }
                    else
                    {
                        CaretConsole.Clear();
                    }
                    Width = 0;
                }
                if (!CaretStarted && UseCaret)
                {
                    CaretConsole.Write("^", CaretColor);
                    if (item == '\t')
                    {
                        CaretConsole.Write(new string('~', 3));
                    }
                    CaretStarted = true;
                }
                else if (UseCaret)
                {
                    CaretConsole.Write(item != '\t' ? "~" : new string('~', 4));
                }
                else
                {
                    CaretConsole.Write(item != '\t' ? " " : new string(' ', 4));
                }
                if (item == '\t')
                {
                    WriteUnsafe(new string(' ', 4));
                }
                else
                {
                    WriteUnsafe(item);
                }
                
            }
            foreach (var item in Node.Children)
            {
                WriteSourceNode(item, CaretStarted, UseCaret, CaretConsole, Indentation, MaxWidth, CaretColor, HighlightColor, ref Width);
            }
        }

        #endregion

        #endregion

        #region WriteEntry

        public void WriteEntry(LogEntry Entry, Color CaretColor, Color HighlightColor)
        {
            lock (writeLock)
            {
                Write(Entry.Name);
                Write(": ");
                WriteNode(Entry.Contents, CaretColor, HighlightColor);
            }
        }

        #endregion

        public void LogError(LogEntry Entry)
        {
            WriteBlockEntry("Error", DefaultConsole.ToPixieColor(ConsoleColor.Red), DefaultConsole.ToPixieColor(ConsoleColor.DarkRed), Entry);
        }

        public void LogEvent(LogEntry Entry)
        {
            WriteWhiteline();
            WriteEntry(Entry, DefaultConsole.ToPixieColor(ConsoleColor.Green), DefaultConsole.ToPixieColor(ConsoleColor.DarkGreen));
            WriteWhiteline();
        }

        public void LogMessage(LogEntry Entry)
        {
            WriteBlockEntry(Entry);
        }

        public void LogWarning(LogEntry Entry)
        {
            WriteBlockEntry("Warning", DefaultConsole.ToPixieColor(ConsoleColor.Yellow), DefaultConsole.ToPixieColor(ConsoleColor.DarkYellow), Entry);
        }

        public void Dispose()
        {
            Console.Dispose();
        }
    }
}
