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
                WriteWhiteline();
            }
        }
        public void WriteBlockEntry(string Header, Color HeaderColor, string Entry)
        {
            lock (writeLock)
            {
                WriteWhiteline();
                Write(Header + ": ", HeaderColor);
                WriteLine(Entry);
                WriteWhiteline();
            }
        }
        public void WriteBlockEntry(string Header, LogEntry Entry)
        {
            lock (writeLock)
            {
                WriteWhiteline();
                Write(Header + ": ");
                WriteEntry(Entry, DefaultConsole.ToPixieColor(ConsoleColor.Green), DefaultConsole.ToPixieColor(ConsoleColor.DarkGreen));
                WriteWhiteline();
            }
        }
        public void WriteBlockEntry(LogEntry Entry)
        {
            WriteWhiteline();
            WriteEntry(Entry, DefaultConsole.ToPixieColor(ConsoleColor.Green), DefaultConsole.ToPixieColor(ConsoleColor.DarkGreen));
            WriteWhiteline();
        }
        public void WriteBlockEntry(string Header, string Entry)
        {
            lock (writeLock)
            {
                WriteWhiteline();
                Write(Header + ": ");
                WriteLine(Entry);
                WriteWhiteline();
            }
        }
        public void WriteErrorBlock(string Header, string Message)
        {
            WriteBlockEntry(Header, DefaultConsole.ToPixieColor(ConsoleColor.Red), Message);
        }

        #region Write*Node

        public void WriteNode(IMarkupNode Node)
        {
            lock (writeLock)
            {
                WriteSourceNode(Node);
            }
        }

        private void WriteNodeCore(IMarkupNode Node)
        {
            if (Node.Type == NodeConstants.SourceNodeType)
            {
                WriteWhiteline();
                WriteSourceNode(Node);
                WriteWhiteline();
            }
            else if (Node.Type == NodeConstants.RemarksNodeType)
            {
                WriteWhiteline();
                Console.PushStyle(new Style("remarks", new Color(0.3), new Color()));
                WriteNode(Node);
                Console.PopStyle();
                WriteWhiteline();
            }
            else
            {
                WriteLine(Node.GetText());
                foreach (var item in Node.Children)
                {
                    WriteNodeCore(item);
                }
            }
        }

        #region WriteSourceNode

        private void WriteSourceNode(IMarkupNode Node)
        {
            int width = 0;
            var caret = new IndirectConsole(Console.Description);
            string indent = new string(' ', 4);
            int bufWidth = BufferWidth - indent.Length - 4;
            WriteSourceNode(Node, false, false, caret, indent, bufWidth, ref width);
            if (width > 0)
            {
                caret.Flush(Console);
            }
        }

        private void WriteSourceNode(IMarkupNode Node, bool CaretStarted, bool UseCaret, IndirectConsole CaretConsole,
            string Indentation, int MaxWidth, ref int Width)
        {
            if (Node.Type.Equals(NodeConstants.HighlightNodeType))
            {
                UseCaret = true;
            }
            string nodeText = Node.GetText();
            foreach (var item in nodeText)
            {
                Width += item == '\t' ? 4 : 1;
                if (Width >= MaxWidth)
                {
                    WriteLine();
                    Write(Indentation);
                    if (!CaretConsole.IsWhitespace)
                    {
                        CaretConsole.Flush(Console);
                        WriteLine();
                        Write(Indentation);
                    }
                    else
                    {
                        CaretConsole.Clear();
                    }
                    Width = 0;
                }
                string caretString;
                if (CaretStarted && UseCaret)
                {
                    caretString = item != '\t' ? "^" : "^~~~";
                    CaretStarted = false;
                }
                else if (UseCaret)
                {
                    caretString = item != '\t' ? "~" : new string('~', 4);
                }
                else
                {
                    caretString = item != '\t' ? " " : new string(' ', 4);
                }
                if (item == '\t')
                {
                    WriteUnsafe(new string(' ', 4));
                }
                else
                {
                    WriteUnsafe(item);
                }
                CaretConsole.Write(caretString);
            }
            foreach (var item in Node.Children)
            {
                WriteSourceNode(item, CaretStarted, UseCaret, CaretConsole, Indentation, MaxWidth, ref Width);
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
                WriteNode(Entry.Contents);
            }
        }

        /*public void WriteEntry(LogEntry Entry, Color CaretColor, Color HighlightColor)
        {
            lock (writeLock)
            {
                Write(Entry.Name);
                Write(": ");
                Write(Entry.Message.TrimEnd('.'));
                if (Entry.Location != null && Entry.Location.Document != null && Entry.Location.Position > -1)
                {
                    Write(',');
                    if (Entry.Location.Document == null)
                    {
                        if (Entry.Location.Position > -1)
                        {
                            Write(" at position ");
                            Write(Entry.Location.Position);
                            Write(" in an unidentified source document.");
                        }
                    }
                    else
                    {
                        var doc = Entry.Location.Document;
                        Write(" in '");
                        Write(Entry.Location.Document.Identifier);
                        Write("'");
                        if (Entry.Location.Position > -1)
                        {
                            var gridPos = Entry.Location.GridPosition;
                            Write(" on line ");
                            Write(gridPos.Line + 1);
                            Write(", column ");
                            Write(gridPos.Offset + 1);
                            Write('.');
                            WriteLine();
                            WriteCaretDiagnostic(Entry, gridPos, CaretColor, HighlightColor);
                        }
                        else
                        {
                            Write(" in '");
                            Write(doc.Identifier);
                            Write("\'.");
                        }
                    }
                }
                else
                {
                    Write('.');
                }
                WriteLine();
            }
        }

        private void WriteCaretDiagnostic(LogEntry Entry, SourceGridPosition GridPosition, ConsoleColor CaretColor, ConsoleColor HighlightColor)
        {
            WriteWhiteline();
            string indent = new string(' ', 4);
            int bufWidth = BufferWidth - indent.Length - 4;
            var annotated = AnnotateSource(Entry, GridPosition, bufWidth);
            WriteCaretLines(annotated.Key, annotated.Value, indent, CaretColor, HighlightColor);
        }

        private void WriteCaretLines(IReadOnlyList<string> Lines, IReadOnlyList<string> Annotations, string Indentation, ConsoleColor CaretColor, ConsoleColor HighlightColor)
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                Write(Indentation);
                Write(Lines[i].TrimEnd());
                WriteLine();
                if (!string.IsNullOrWhiteSpace(Annotations[i]))
                {
                    Write(Indentation);
                    foreach (var character in Annotations[i])
                    {
                        if (character == '^')
                        {
                            Write("^", CaretColor);
                        }
                        else
                        {
                            Write(character.ToString(), HighlightColor);
                        }
                    }
                    WriteLine();
                }
            }
        }

        private static KeyValuePair<IReadOnlyList<string>, IReadOnlyList<string>> AnnotateSource(LogEntry Entry, SourceGridPosition GridPosition, int BufferWidth)
        {
            var loc = Entry.Location;
            var doc = loc.Document;
            string lineSource = doc.GetLine(GridPosition.Line);
            int highlightCount = Math.Max(0, Math.Min(loc.Length - 1, lineSource.Length - GridPosition.Offset));
            StringBuilder formattedLineSource = new StringBuilder();
            StringBuilder formattedCaret = new StringBuilder();
            int i;
            for (i = 0; i < lineSource.Length && char.IsWhiteSpace(lineSource[i]); i++) ;
            for (; i < lineSource.Length; i++)
            {
                if (lineSource[i] == '\t')
                {
                    formattedLineSource.Append(new string(' ', 4));
                    formattedCaret.Append(new string(GetCaretCharacter(GridPosition, i, highlightCount), 4));
                }
                else
                {
                    formattedLineSource.Append(lineSource[i]);
                    formattedCaret.Append(GetCaretCharacter(GridPosition, i, highlightCount));
                }
            }
            var splitSource = SplitLength(formattedLineSource.ToString(), BufferWidth);
            var splitCaret = SplitLength(formattedCaret.ToString(), BufferWidth);
            return new KeyValuePair<IReadOnlyList<string>, IReadOnlyList<string>>(splitSource, splitCaret);
        }

        private static char GetCaretCharacter(SourceGridPosition GridPosition, int Offset, int Length)
        {
            if (Offset == GridPosition.Offset)
            {
                return '^';
            }
            else if (Offset > GridPosition.Offset && Offset - GridPosition.Offset <= Length)
            {
                return '~';
            }
            else
            {
                return ' ';
            }
        }

        private static IReadOnlyList<string> SplitLength(string Value, int Width)
        {
            List<string> results = new List<string>();
            int breaks = Value.Length / Width;
            for (int i = 0; i < breaks; i++)
            {
                results.Add(Value.Substring(i * Width, Width));
            }
            results.Add(Value.Substring(breaks * Width));
            return results;
        }*/

        #endregion

        public void LogError(LogEntry Entry)
        {
            WriteBlockEntry("Error", DefaultConsole.ToPixieColor(ConsoleColor.Red), DefaultConsole.ToPixieColor(ConsoleColor.DarkRed), Entry);
        }

        public void LogEvent(LogEntry Entry)
        {
            WriteEntry(Entry, DefaultConsole.ToPixieColor(ConsoleColor.Green), DefaultConsole.ToPixieColor(ConsoleColor.DarkGreen));
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
