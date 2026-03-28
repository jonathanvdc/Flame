using System.Collections.Immutable;
using Pixie;
using Pixie.Loyc;
using Pixie.Markup;
using Pixie.Transforms;

namespace UnitTests
{
    /// <summary>
    /// Utility methods for setting up tests.
    /// </summary>
    internal static class TestUtils
    {
        /// <summary>
        /// Creates a test log that throws on errors.
        /// </summary>
        public static ILog CreateTestLog()
        {
            var rawLog = Pixie.Terminal.TerminalLog.Acquire();
            var ioLog = new TransformLog(
                rawLog,
                entry => DiagnosticExtractor.Transform(entry, new Text("unit-tests")));
            return new TestLog(
                ImmutableHashSet<Pixie.Severity>.Empty.Add(Pixie.Severity.Error),
                ioLog);
        }
    }
}
