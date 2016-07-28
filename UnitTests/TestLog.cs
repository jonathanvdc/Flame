using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    public enum Severity
    {
        Error,
        Warning,
        Message,
        Event
    }

    [Serializable]
    public class EntryException : Exception
    {
        public EntryException(LogEntry Entry) { this.Entry = Entry; }
        public EntryException(LogEntry Entry, string message) : base(message) { this.Entry = Entry; }
        public EntryException(LogEntry Entry, string message, Exception inner) : base(message, inner) { this.Entry = Entry; }
        protected EntryException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public LogEntry Entry { get; private set; }
    }

    public class TestLog : ICompilerLog
    {
        public TestLog(ICompilerOptions Options)
            : this(Options, true)
        {
        }
        public TestLog(ICompilerOptions Options, bool ThrowErrors)
        {
            this.Options = Options;
            this.ThrowErrors = ThrowErrors;
            this.Entries = new List<Tuple<Severity, LogEntry>>();            
        }

        public ICompilerOptions Options { get; private set; }
        public bool ThrowErrors { get; private set; }
        public List<Tuple<Severity, LogEntry>> Entries { get; private set; }

        private IEnumerable<LogEntry> WithSeverity(Severity Level)
        {
            return Entries.Where(item => item.Item1 == Level).Select(item => item.Item2);
        }

        public IEnumerable<LogEntry> Errors
        {
            get
            {
                return WithSeverity(Severity.Error);
            }
        }

        public IEnumerable<LogEntry> Warnings
        {
            get
            {
                return WithSeverity(Severity.Warning);
            }
        }

        public IEnumerable<LogEntry> Messages
        {
            get
            {
                return WithSeverity(Severity.Message);
            }
        }

        public IEnumerable<LogEntry> Events
        {
            get
            {
                return WithSeverity(Severity.Event);
            }
        }

        public void LogEntry(Severity Severity, LogEntry Entry)
        {
            Entries.Add(Tuple.Create(Severity, Entry));
        }

        public void LogError(LogEntry Entry)
        {
            LogEntry(Severity.Error, Entry);
            if (ThrowErrors)
            {
                throw new EntryException(Entry);
            }
        }

        public void LogEvent(LogEntry Entry)
        {
            LogEntry(Severity.Event, Entry);
        }

        public void LogMessage(LogEntry Entry)
        {
            LogEntry(Severity.Message, Entry);
        }

        public void LogWarning(LogEntry Entry)
        {
            LogEntry(Severity.Warning, Entry);
        }

        public void Dispose()
        {
        }
    }
}
