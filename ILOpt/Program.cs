using System;
using System.IO;
using System.Linq;
using Flame.Clr;
using Flame.Clr.Emit;
using Flame.Clr.Transforms;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Transforms;
using Flame.Ir;
using Flame.TypeSystem;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Pixie;
using Pixie.Markup;
using Pixie.Options;
using Pixie.Terminal;
using Pixie.Transforms;

namespace ILOpt
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

            var outputPath = parsedOptions.GetValue<string>(Options.Output);
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.GetFileNameWithoutExtension(inputPath) + ".opt" + Path.GetExtension(inputPath);
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
                // Bootstrap a type system resolver.
                var typeSystem = new MutableTypeEnvironment(null);
                var resolver = new CecilAssemblyResolver(
                    cecilAsm.MainModule.AssemblyResolver,
                    typeSystem);
                var flameAsm = new ClrAssembly(cecilAsm, resolver.ReferenceResolver);

                var objectType = flameAsm.Resolve(cecilAsm.MainModule.TypeSystem.Object);
                var corlib = objectType.Parent.Assembly;

                typeSystem.InnerEnvironment = new CorlibTypeEnvironment(corlib);

                // Optimize the assembly.
                OptimizeAssembly(cecilAsm, flameAsm, def => OptimizeBody(def, typeSystem, printIr));

                // Write the optimized assembly to disk.
                cecilAsm.Write(outputPath);
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
                .Transform(entry, "ilopt")
                .Map(WrapBox.WordWrap);
        }

        private static void OptimizeAssembly(
            Mono.Cecil.AssemblyDefinition cecilAsm,
            ClrAssembly flameAsm,
            Func<ClrMethodDefinition, MethodBody> optimizeBody)
        {
            foreach (var module in cecilAsm.Modules)
            {
                foreach (var type in module.Types)
                {
                    OptimizeType(type, flameAsm, optimizeBody);
                }
            }
        }

        private static void OptimizeType(
            Mono.Cecil.TypeDefinition typeDefinition,
            ClrAssembly parentAssembly,
            Func<ClrMethodDefinition, MethodBody> optimizeBody)
        {
            foreach (var method in typeDefinition.Methods)
            {
                OptimizeMethod(method, parentAssembly, optimizeBody);
            }
            foreach (var type in typeDefinition.NestedTypes)
            {
                OptimizeType(type, parentAssembly, optimizeBody);
            }
        }

        private static void OptimizeMethod(
            Mono.Cecil.MethodDefinition methodDefinition,
            ClrAssembly parentAssembly,
            Func<ClrMethodDefinition, MethodBody> optimizeBody)
        {
            if (methodDefinition.HasBody)
            {
                var flameMethod = (ClrMethodDefinition)parentAssembly.Resolve(methodDefinition);
                var irBody = flameMethod.Body;

                var errors = irBody.Validate();
                if (errors.Count > 0)
                {
                    var sourceIr = FormatIr(irBody);
                    log.Log(
                        new LogEntry(
                            Severity.Warning,
                            "invalid IR",
                            Quotation.QuoteEvenInBold(
                                "the Flame IR produced by the CIL analyzer for ",
                                flameMethod.FullName.ToString(),
                                " is erroneous; skipping it."),

                            CreateRemark(
                                "errors in IR:",
                                new BulletedList(errors.Select(x => new Text(x)).ToArray())),

                            CreateRemark(
                                "generated Flame IR:",
                                new Paragraph(new WrapBox(sourceIr, 0, -sourceIr.Length)))));
                    return;
                }

                var optBody = optimizeBody(flameMethod);
                var emitter = new ClrMethodBodyEmitter(
                    methodDefinition,
                    optBody,
                    parentAssembly.Resolver.TypeEnvironment);
                var newCilBody = emitter.Compile();
                methodDefinition.Body = newCilBody;
            }
        }

        private static MethodBody OptimizeBody(
            ClrMethodDefinition method,
            TypeEnvironment typeSystem,
            bool printIr)
        {
            var irBody = method.Body;

            // Register analyses.
            irBody = new MethodBody(
                irBody.ReturnParameter,
                irBody.ThisParameter,
                irBody.Parameters,
                irBody.Implementation
                    .WithAnalysis(LazyBlockReachabilityAnalysis.Instance)
                    .WithAnalysis(NullabilityAnalysis.Instance)
                    .WithAnalysis(new EffectfulInstructionAnalysis())
                    .WithAnalysis(PredecessorAnalysis.Instance)
                    .WithAnalysis(RelatedValueAnalysis.Instance)
                    .WithAnalysis(LivenessAnalysis.Instance)
                    .WithAnalysis(InterferenceGraphAnalysis.Instance)
                    .WithAnalysis(ValueUseAnalysis.Instance)
                    .WithAnalysis(ConservativeInstructionOrderingAnalysis.Instance)
                    .WithAnalysis(DominatorTreeAnalysis.Instance)
                    .WithAnalysis(ValueNumberingAnalysis.Instance)
                    .WithAnalysis(
                        new ConstantAnalysis<SubtypingRules>(
                            typeSystem.Subtyping))
                    .WithAnalysis(
                        new ConstantAnalysis<PermissiveExceptionDelayability>(
                            PermissiveExceptionDelayability.Instance)));

            // Optimize the IR a tiny bit.
            irBody = irBody.WithImplementation(
                irBody.Implementation.Transform(
                    // Optimization passes.
                    //   * Initial CFG cleanup.
                    AllocaToRegister.Instance,
                    CopyPropagation.Instance,
                    new ConstantPropagation(),
                    CanonicalizeDelegates.Instance,
                    InstructionSimplification.Instance,

                    //   * Box to alloca, alloca to reg.
                    BoxToAlloca.Instance,
                    CopyPropagation.Instance,
                    AllocaToRegister.Instance,

                    //   * Aggregates to scalars, scalars to registers.
                    //     Also throw in GVN.
                    DeadValueElimination.Instance,
                    ScalarReplacement.Instance,
                    GlobalValueNumbering.Instance,
                    CopyPropagation.Instance,
                    DeadValueElimination.Instance,
                    AllocaToRegister.Instance,

                    //   * Optimize control flow.
                    InstructionSimplification.Instance,
                    new ConstantPropagation(),
                    SwitchSimplification.Instance,
                    DeadValueElimination.Instance,
                    new JumpThreading(true),
                    DuplicateReturns.Instance,
                    new TailRecursionElimination(method),
                    BlockFusion.Instance,

                    // Lowering and cleanup passes.
                    JumpToEntryRemoval.Instance,
                    DeadBlockElimination.Instance,
                    new SwitchLowering(typeSystem),
                    CopyPropagation.Instance,
                    FuseMemoryAccesses.Instance,
                    DeadValueElimination.Instance,
                    new JumpThreading(false),
                    LowerDelegates.Instance));

            if (printIr)
            {
                PrintIr(method, method.Body, irBody);
            }

            return irBody;
        }

        private static void PrintIr(
            ClrMethodDefinition method,
            MethodBody sourceBody,
            MethodBody optBody)
        {
            var sourceIr = FormatIr(sourceBody);
            var optIr = FormatIr(optBody);

            log.Log(
                new LogEntry(
                    Severity.Message,
                    "method body IR",
                    "optimized Flame IR for ",
                    Quotation.CreateBoldQuotation(method.FullName.ToString()),
                    ": ",
                    new Paragraph(new WrapBox(optIr, 0, -optIr.Length)),
                    CreateRemark(
                        "unoptimized Flame IR:",
                        new Paragraph(new WrapBox(sourceIr, 0, -sourceIr.Length)))));
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

        private static MarkupNode CreateRemark(
            params MarkupNode[] contents)
        {
            return new Paragraph(
                new MarkupNode[] { DecorationSpan.MakeBold(new ColorSpan("remark: ", Colors.Gray)) }
                .Concat(contents)
                .ToArray());
        }
    }
}
