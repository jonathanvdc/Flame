using System.Collections.Immutable;
using Loyc.MiniTest;
using Pixie;

namespace UnitTests
{
    /// <summary>
    /// A type of log that sends messages to another log and aborts
    /// execution if the severity of an error is deemed fatal
    /// </summary>
    public class TestLog : ILog
    {
        public TestLog(
            ImmutableHashSet<Severity> fatalSeverities,
            ILog redirectionLog)
        {
            this.FatalSeverities = fatalSeverities;
            this.RedirectionLog = redirectionLog;
        }

        /// <summary>
        /// Gets the set of all severities that are considered fatal
        /// by this log. A log entry whose severity is fatal triggers
        /// an exception.
        /// </summary>
        /// <returns>An immutable hash set.</returns>
        public ImmutableHashSet<Severity> FatalSeverities { get; private set; }

        /// <summary>
        /// Gets the log to which messages are sent by this log before
        /// the decision to abort the program or not is taken.
        /// </summary>
        /// <returns>The inner log.</returns>
        public ILog RedirectionLog { get; private set; }

        /// <inheritdoc/>
        public void Log(LogEntry entry)
        {
            RedirectionLog.Log(entry);
            if (FatalSeverities.Contains(entry.Severity))
            {
                Assert.Fail();
            }
        }
    }
}