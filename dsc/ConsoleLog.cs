using dsc.Options;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public sealed class ConsoleLog : ICompilerLog
    {
        private ConsoleLog(ICompilerOptions Options)
        {
            this.Options = Options;
            this.gapQueued = false;
            this.gapLock = new object();
        }

        public ICompilerOptions Options { get; private set; }
        private bool gapQueued;
        private object gapLock;

        private void WriteGap()
        {
            lock (gapLock)
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

        public void WriteWhiteline()
        {
            lock (gapLock)
            {
                gapQueued = true;
            }
        }
        public void Write(string Text, ConsoleColor Color)
        {
            WriteGap();
            Console.ForegroundColor = Color;
            WriteInternal(Text);
            Console.ResetColor();
        }
        public void Write(string Text)
        {
            WriteGap();
            WriteInternal(Text);
        }
        public void Write(char Value)
        {
            WriteGap();
            WriteInternal(Value);
        }
        public void Write<T>(T Value)
        {
            Write(Value.ToString());
        }
        public void WriteLine(string Text, ConsoleColor Color)
        {
            WriteGap();
            Console.ForegroundColor = Color;
            WriteLineInternal(Text);
            Console.ResetColor();
        }
        public void WriteLine()
        {
            WriteGap();
            WriteLineInternal();
        }
        public void WriteLine(string Text)
        {
            WriteGap();
            WriteLineInternal(Text);
        }
        public void WriteLine<T>(T Value)
        {
            WriteLine(Value.ToString());
        }
        public void WriteBlockEntry(string Header, ConsoleColor HeaderColor, LogEntry Entry)
        {
            WriteWhiteline();
            Write(Header + ": ", HeaderColor);
            WriteEntry(Entry);
            WriteWhiteline();
        }
        public void WriteBlockEntry(string Header, ConsoleColor HeaderColor, string Entry)
        {
            WriteWhiteline();
            Write(Header + ": ", HeaderColor);
            WriteLine(Entry);
            WriteWhiteline();
        }
        public void WriteBlockEntry(string Header, LogEntry Entry)
        {
            WriteWhiteline();
            Write(Header + ": ");
            WriteEntry(Entry);
            WriteWhiteline();
        }
        public void WriteBlockEntry(string Header, string Entry)
        {
            WriteWhiteline();
            Write(Header + ": ");
            WriteLine(Entry);
            WriteWhiteline();
        }
        public void WriteErrorBlock(string Header, string Message)
        {
            WriteBlockEntry(Header, ConsoleColor.Red, Message);
        }

        private static ConsoleLog inst;
        public static ConsoleLog Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new ConsoleLog(new StringCompilerOptions());
                }
                return inst;
            }
        }

        #region WriteEntry

        public void WriteEntry(LogEntry Entry)
        {
            Write(Entry.Name);
            Write(": ");
            Write(Entry.Message);
            if (Entry.Location != null && Entry.Location.Document != null && Entry.Location.Position > -1)
            {
                Write(",");
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
                        WriteCaretDiagnostic(Entry, gridPos);
                    }
                    else
                    {
                        Write(" in '");
                        Write(doc.Identifier);
                        Write("\'.");
                    }
                }
            }
            WriteLine();
        }

        private void WriteCaretDiagnostic(LogEntry Entry, SourceGridPosition GridPosition)
        {
            var loc = Entry.Location;
            var doc = loc.Document;
            string lineSource = doc.GetLine(GridPosition.Line);
            WriteWhiteline();
            Write(lineSource);
            WriteLine();
            for (int i = 0; i < GridPosition.Offset; i++)
            {
                if (lineSource[i] == '\t')
                {
                    Write('\t');
                }
                else
                {
                    Write(' ');
                }
            }
            Write("^", ConsoleColor.Green);
            int highlightCount = Math.Max(0, Math.Min(loc.Length - 1, lineSource.Length - GridPosition.Offset));
            Write(new string('~', highlightCount), ConsoleColor.DarkGreen);
        }

        #endregion

        public void LogError(LogEntry Entry)
        {
            WriteBlockEntry("Error", ConsoleColor.Red, Entry);
        }

        public void LogEvent(LogEntry Entry)
        {
            WriteEntry(Entry);
        }

        public void LogMessage(LogEntry Entry)
        {
            WriteBlockEntry("Message", Entry);
        }

        public void LogWarning(LogEntry Entry)
        {
            WriteBlockEntry("Warning", ConsoleColor.Yellow, Entry);
        }

        public void Dispose()
        {
        }
    }
}
