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
            this.Palette = new StylePalette(Console.Description);
            this.gapQueued = false;
            this.writeLock = new object();
        }

        public IConsole Console { get; private set; }
        public ICompilerOptions Options { get; private set; }
        public IStylePalette Palette { get; private set; }
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
                WriteEntry(Entry, ContrastGreen, DimGreen);
                WriteSeparator();
            }
        }
        public void WriteBlockEntry(LogEntry Entry)
        {
            lock (writeLock)
            {
                WriteWhiteline();
                WriteEntry(Entry, ContrastGreen, DimGreen);
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
            WriteBlockEntry(Header, ContrastRed, Message);
        }

        #region Palette

        public Color ContrastRed
        {
            get
            {
                return Palette.MakeContrastColor(DefaultConsole.ToPixieColor(ConsoleColor.Red));
            }
        }

        public Color DimRed
        {
            get
            {
                return Palette.MakeDimColor(DefaultConsole.ToPixieColor(ConsoleColor.Red));
            }
        }

        public Color ContrastYellow
        {
            get
            {
                return Palette.MakeContrastColor(DefaultConsole.ToPixieColor(ConsoleColor.Yellow));
            }
        }

        public Color DimYellow
        {
            get
            {
                return Palette.MakeDimColor(DefaultConsole.ToPixieColor(ConsoleColor.Yellow));
            }
        }

        public Color ContrastBlue
        {
            get
            {
                return Palette.MakeContrastColor(DefaultConsole.ToPixieColor(ConsoleColor.Blue));
            }
        }

        public Color DimBlue
        {
            get
            {
                return Palette.MakeDimColor(DefaultConsole.ToPixieColor(ConsoleColor.Blue));
            }
        }

        public Color ContrastGreen
        {
            get
            {
                return Palette.MakeContrastColor(DefaultConsole.ToPixieColor(ConsoleColor.Green));
            }
        }

        public Color DimGreen
        {
            get
            {
                return Palette.MakeDimColor(DefaultConsole.ToPixieColor(ConsoleColor.Green));
            }
        }

        public Color ContrastGray
        {
            get
            {
                return Palette.MakeContrastColor(DefaultConsole.ToPixieColor(ConsoleColor.Gray));
            }
        }

        public Color DimGray
        {
            get
            {
                return Palette.MakeDimColor(DefaultConsole.ToPixieColor(ConsoleColor.Gray));
            }
        }

        #endregion

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
                Console.PushStyle(new Style("remarks", DimGray, new Color()));
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
            var highlightingStyle = new Style("caret-highlight", HighlightColor, new Color());
            var caretStyle = new Style("caret-marker", CaretColor, new Color());
            string indent = new string(' ', 4);
            int bufWidth = BufferWidth - indent.Length - 4;
            WriteWhiteline();
            using (var writer = new SourceNodeWriter(Console, caretStyle, highlightingStyle, indent, bufWidth))
            {
                writer.Write(Node);
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
            WriteBlockEntry("Error", ContrastRed, DimRed, Entry);
        }

        public void LogEvent(LogEntry Entry)
        {
            WriteWhiteline();
            WriteEntry(Entry, ContrastGreen, DimGreen);
            WriteWhiteline();
        }

        public void LogMessage(LogEntry Entry)
        {
            WriteBlockEntry(Entry);
        }

        public void LogWarning(LogEntry Entry)
        {
            WriteBlockEntry("Warning", ContrastYellow, DimYellow, Entry);
        }

        public void Dispose()
        {
            Console.Dispose();
        }
    }
}
