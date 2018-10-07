using System;
using Pixie;
using Pixie.Markup;
using Pixie.Options;
using Pixie.Terminal;
using Pixie.Transforms;

namespace ILOpt
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            // Acquire a log.
            var rawLog = TerminalLog.Acquire();
            var log = new TransformLog(
                rawLog,
                new Func<LogEntry, LogEntry>[]
                {
                    MakeDiagnostic
                });

            // Parse command-line options.
            var parser = new GnuOptionSetParser(
                Options.All, Options.Input);

            var parsedOptions = parser.Parse(args, log);

            if (parsedOptions.GetValue<bool>(Options.Help))
            {
                // Wrap the help message into a log entry and send it to the log.
                rawLog.Log(
                    new LogEntry(
                        Severity.Info,
                        new HelpMessage(
                            "ilopt is a command-line tool that optimizes CIL assemblies.",
                            "ilopt path [options...]",
                            Options.All)));
                return 0;
            }

            var inputPath = parsedOptions.GetValue<string>(Options.Input);
            if (string.IsNullOrEmpty(inputPath))
            {
                log.Log(
                    new LogEntry(
                        Severity.Error,
                        "nothing to optimize",
                        "no input file"));
                return 1;
            }

            return 0;
        }

        private static LogEntry MakeDiagnostic(LogEntry entry)
        {
            return DiagnosticExtractor
                .Transform(entry, "ilopt")
                .Map(WrapBox.WordWrap);
        }
    }
}
