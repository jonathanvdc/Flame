using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flame;
using Flame.Clr;
using Flame.Clr.Transforms;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Pipeline;
using Flame.Compiler.Transforms;
using Flame.Ir;
using Flame.Llvm;
using Flame.TypeSystem;
using LLVMSharp;
using Loyc.Syntax;
using Loyc.Syntax.Les;
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

            if (cecilAsm.EntryPoint == null)
            {
                log.Log(
                    new LogEntry(
                        Severity.Error,
                        "unsuitable assembly",
                        "input assembly does not define an entry point."));
                return 1;
            }

            try
            {
                // Wrap the CIL assembly in a Flame assembly.
                var flameAsm = ClrAssembly.Wrap(cecilAsm);

                // Compile the assembly to an LLVM module.
                var module = CompileAsync(flameAsm).Result;

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

        private static async Task<LLVMModuleRef> CompileAsync(ClrAssembly assembly)
        {
            var desc = await CreateContentDescriptionAsync(assembly);
            return LlvmBackend.Compile(desc, assembly.Resolver.TypeEnvironment);
        }

        private static Task<AssemblyContentDescription> CreateContentDescriptionAsync(ClrAssembly assembly)
        {
            var typeSystem = assembly.Resolver.TypeEnvironment;
            var pipeline = new Optimization[]
            {
                new ConstantPropagation(),
                MemoryAccessElimination.Instance,
                DeadValueElimination.Instance,
                new JumpThreading(true),
                SwitchSimplification.Instance,
                DuplicateReturns.Instance,
                TailRecursionElimination.Instance,
                BlockFusion.Instance
            };

            var optimizer = new OnDemandOptimizer(
                pipeline,
                method => GetInitialMethodBody(method, typeSystem));

            return AssemblyContentDescription.CreateTransitiveAsync(
                assembly.FullName,
                assembly.Attributes,
                assembly.Resolve(assembly.Definition.EntryPoint),
                optimizer);
        }

        private static MethodBody GetInitialMethodBody(IMethod method, TypeEnvironment typeSystem)
        {
            var body = OnDemandOptimizer.GetInitialMethodBodyDefault(method);
            if (body == null)
            {
                return null;
            }

            // Validate the method body.
            var errors = body.Validate();
            if (errors.Count > 0)
            {
                var sourceIr = FormatIr(body);
                log.Log(
                    new LogEntry(
                        Severity.Warning,
                        "invalid IR",
                        Quotation.QuoteEvenInBold(
                            "the Flame IR produced by the CIL analyzer for ",
                            method.FullName.ToString(),
                            " is erroneous; skipping it."),

                        CreateRemark(
                            "errors in IR:",
                            new BulletedList(errors.Select(x => new Text(x)).ToArray())),

                        CreateRemark(
                            "generated Flame IR:",
                            new Paragraph(new WrapBox(sourceIr, 0, -sourceIr.Length)))));
                return null;
            }

            // Register some analyses and clean up the CFG before we actually start to optimize it.
            return body.WithImplementation(
                body.Implementation
                    .WithAnalysis(
                        new ConstantAnalysis<SubtypingRules>(
                            typeSystem.Subtyping))
                    .WithAnalysis(
                        new ConstantAnalysis<PermissiveExceptionDelayability>(
                            PermissiveExceptionDelayability.Instance))
                    .Transform(
                        AllocaToRegister.Instance,
                        CopyPropagation.Instance,
                        new ConstantPropagation(),
                        CanonicalizeDelegates.Instance,
                        InstructionSimplification.Instance));
        }

        private static LogEntry MakeDiagnostic(LogEntry entry)
        {
            return DiagnosticExtractor
                .Transform(entry, "il2llvm")
                .Map(WrapBox.WordWrap);
        }

        private static MarkupNode CreateRemark(
            params MarkupNode[] contents)
        {
            return new Paragraph(
                new MarkupNode[] { DecorationSpan.MakeBold(new ColorSpan("remark: ", Colors.Gray)) }
                .Concat(contents)
                .ToArray());
        }

        private static string FormatIr(MethodBody methodBody)
        {
            var encoder = new EncoderState();
            var encodedImpl = encoder.Encode(methodBody.Implementation);

            return Les2LanguageService.Value.Print(
                encodedImpl,
                options: new LNodePrinterOptions
                {
                    IndentString = new string(' ', 4)
                });
        }
    }
}
