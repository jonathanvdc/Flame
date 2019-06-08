using System;
using System.IO;
using Flame.Clr;
using Flame.Llvm;
using LLVMSharp;
using Pixie;
using Pixie.Markup;
using Pixie.Options;
using Pixie.Terminal;
using Pixie.Transforms;

namespace IL2LLVM
{
    public static class Program
    {
        private static ILog log;

        public static int Main(string[] args)
        {
            // Acquire a log.
            var rawLog = TerminalLog.Acquire();
            log = new TransformLog(
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
                TerminalLog.AcquireStandardOutput().Log(
                    new LogEntry(
                        Severity.Info,
                        new HelpMessage(
                            "il2llvm is a command-line tool that compiles CIL assemblies to LLVM modules.",
                            "il2llvm path [options...]",
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
                outputPath = Path.GetFileNameWithoutExtension(inputPath) + ".ll";
            }

            var printIr = parsedOptions.GetValue<bool>(Options.PrintIr);

            // Read the assembly from disk.
            Mono.Cecil.AssemblyDefinition cecilAsm;
            try
            {
                cecilAsm = Mono.Cecil.AssemblyDefinition.ReadAssembly(
                    inputPath,
                    new Mono.Cecil.ReaderParameters
                    {
                        ReadWrite = false
                    });
            }
            catch (Exception)
            {
                log.Log(
                    new LogEntry(
                        Severity.Error,
                        "unreadable assembly",
                        Quotation.QuoteEvenInBold(
                            "cannot read assembly at ",
                            inputPath,
                            ".")));
                return 1;
            }

            try
            {
                // Wrap the CIL assembly in a Flame assembly.
                var flameAsm = ClrAssembly.Wrap(cecilAsm);
                var typeSystem = flameAsm.Resolver.TypeEnvironment;

                // Compile the assembly to an LLVM module.
                var module = LlvmBackend.Compile(flameAsm);

                // Write the LLVM module to disk.
                string error;
                if (LLVM.PrintModuleToFile(module, outputPath, out error))
                {
                    log.Log(new LogEntry(Severity.Error, "cannot write module", error));
                }

                LLVM.DisposeModule(module);
            }
            finally
            {
                // Be sure to dispose the assembly after we've used it.
                cecilAsm.Dispose();
            }

            return 0;
        }

        private static LogEntry MakeDiagnostic(LogEntry entry)
        {
            return DiagnosticExtractor
                .Transform(entry, "il2llvm")
                .Map(WrapBox.WordWrap);
        }
    }
}
