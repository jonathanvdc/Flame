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
        }

        public ICompilerOptions Options { get; private set; }

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

        public static string GetEntryString(LogEntry Entry)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Entry.Name);
            sb.Append(": ");
            sb.Append(Entry.Message);
            if (Entry.Location != null && Entry.Location.DocumentPath != null && Entry.Location.Position > -1)
            {
                sb.Append(",");
                if (Entry.Location.DocumentPath != null)
                {
                    sb.Append(" in ");
                    sb.Append(Entry.Location.DocumentPath);
                }
                if (Entry.Location.Position > -1)
                {
                    sb.Append(" at position ");
                    sb.Append(Entry.Location.Position);
                }
            }
            return sb.ToString();
        }

        public void LogError(LogEntry Entry)
        {
            Console.WriteLine("Error: " + GetEntryString(Entry));
        }

        public void LogEvent(LogEntry Entry)
        {
            Console.WriteLine(GetEntryString(Entry));
        }

        public void LogMessage(LogEntry Entry)
        {
            Console.WriteLine(GetEntryString(Entry));
        }

        public void LogWarning(LogEntry Entry)
        {
            Console.WriteLine("Warning: " + GetEntryString(Entry));
        }

        public void Dispose()
        {
        }
    }
}
