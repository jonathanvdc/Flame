using System;
using System.IO;
using Pixie;
using Pixie.Markup;
using Pixie.Options;
using Pixie.Terminal;
using Pixie.Transforms;

namespace Flame.Brainfuck
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

            var recLog = new RecordingLog(log);
            var parsedOptions = parser.Parse(args, recLog);

            if (recLog.Contains(Severity.Error))
            {
                // Stop the program if the command-line arguments
                // are half baked.
                return 1;
            }

            if (parsedOptions.GetValue<bool>(Options.Help))
            {
                // Wrap the help message into a log entry and send it to the log.
                rawLog.Log(
                    new LogEntry(
                        Severity.Info,
                        new HelpMessage(
                            "fbfc is a compiler that turns Brainfuck code into CIL assemblies.",
                            "fbfc path [options...]",
                            Options.All)));
                return 0;
            }

            var inputPath = parsedOptions.GetValue<string>(Options.Input);
            if (string.IsNullOrEmpty(inputPath))
            {
                log.Log(
                    new LogEntry(
                        Severity.Error,
                        "nothing to compile",
                        "no input file"));
                return 1;
            }

            var outputPath = parsedOptions.GetValue<string>(Options.Output);
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.GetFileNameWithoutExtension(inputPath) + ".exe";
            }

            var printIr = parsedOptions.GetValue<bool>(Options.PrintIr);

            return 0;
        }

        private static LogEntry MakeDiagnostic(LogEntry entry)
        {
            return DiagnosticExtractor
                .Transform(entry, "fbfc")
                .Map(WrapBox.WordWrap);
        }
    }
}
