using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Options
{
    public class OptionLog : ICompilerLog
    {
        public OptionLog(ICompilerLog Log, ICompilerOptions Options)
        {
            this.Log = Log;
            this.Options = Options;
        }

        public ICompilerLog Log { get; private set; }
        public ICompilerOptions Options { get; private set; }

        public void LogError(LogEntry Entry)
        {
            Log.LogError(Entry);
        }

        public void LogEvent(LogEntry Entry)
        {
            Log.LogEvent(Entry);
        }

        public void LogMessage(LogEntry Entry)
        {
            Log.LogMessage(Entry);
        }

        public void LogWarning(LogEntry Entry)
        {
            Log.LogWarning(Entry);
        }

        public void Dispose()
        {
            Log.Dispose();
        }
    }

    public static class OptionLogExtensions
    {
        public static ICompilerLog WithOptions(this ICompilerLog Log, ICompilerOptions Options)
        {
            return new OptionLog(Log, Options);
        }
    }
}
