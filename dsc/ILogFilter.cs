using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public interface ILogFilter
    {
        bool ShouldLogError(LogEntry Error);
        bool ShouldLogWarning(LogEntry Warning);
        bool ShouldLogMessage(LogEntry Message);
        bool ShouldLogEvent(LogEntry Status);
    }

    public class FilteredLog : ICompilerLog
    {
        public FilteredLog(ILogFilter Filter, ICompilerLog Log)
        {
            this.Filter = Filter;
            this.Log = Log;
        }

        public ILogFilter Filter { get; private set; }
        public ICompilerLog Log { get; private set; }

        public void LogError(LogEntry Entry)
        {
            if (Filter.ShouldLogError(Entry))
            {
                Log.LogError(Entry);
            }
        }

        public void LogEvent(LogEntry Entry)
        {
            if (Filter.ShouldLogEvent(Entry))
            {
                Log.LogEvent(Entry);
            }
        }

        public void LogMessage(LogEntry Entry)
        {
            if (Filter.ShouldLogMessage(Entry))
            {
                Log.LogMessage(Entry);
            }
        }

        public void LogWarning(LogEntry Entry)
        {
            if (Filter.ShouldLogWarning(Entry))
            {
                Log.LogWarning(Entry);
            }
        }

        public ICompilerOptions Options
        {
            get { return Log.Options; }
        }

        public void Dispose()
        {
            Log.Dispose();
        }
    }
}
