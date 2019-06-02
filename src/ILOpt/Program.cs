using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flame;
using Flame.Clr;
using Flame.Clr.Emit;
using Flame.Clr.Transforms;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Pipeline;
using Flame.Compiler.Transforms;
using Flame.Ir;
using Flame.TypeSystem;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Mono.Cecil;
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
                TerminalLog.AcquireStandardOutput().Log(
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
                // Make all non-public types, methods and fields in the assembly
                // internal if the user requests it. This will work to
                // our advantage.
                if (parsedOptions.GetValue<bool>(Options.Internalize))
                {
                    MakeInternal(cecilAsm);
                }

                // Wrap the CIL assembly in a Flame assembly.
                var flameAsm = ClrAssembly.Wrap(cecilAsm);
                var typeSystem = flameAsm.Resolver.TypeEnvironment;

                // Optimize the assembly.
                OptimizeAssemblyAsync(flameAsm, printIr).Wait();

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

        private static void MakeInternal(AssemblyDefinition assembly)
        {
            foreach (var module in assembly.Modules)
            {
                foreach (var type in module.Types)
                {
                    MakeInternal(type);
                }
            }
        }

        private static void MakeInternal(TypeDefinition type)
        {
            if (type.IsNestedPrivate)
            {
                type.IsNestedPrivate = false;
                type.IsNestedAssembly = true;
            }
            else if (type.IsNestedFamily)
            {
                type.IsNestedFamily = false;
                type.IsNestedFamilyOrAssembly = true;
            }
            foreach (var method in type.Methods)
            {
                if (method.IsPrivate)
                {
                    method.IsPrivate = false;
                    method.IsAssembly = true;
                }
                else if (method.IsFamily)
                {
                    method.IsFamily = false;
                    method.IsAssembly = true;
                }
            }
            foreach (var field in type.Fields)
            {
                if (field.IsPrivate)
                {
                    field.IsPrivate = false;
                    field.IsAssembly = true;
                }
                else if (field.IsFamily)
                {
                    field.IsFamily = false;
                    field.IsAssembly = true;
                }
            }
            foreach (var nestedType in type.NestedTypes)
            {
                MakeInternal(nestedType);
            }
        }

        private static Task OptimizeAssemblyAsync(ClrAssembly assembly, bool printIr)
        {
            var typeSystem = assembly.Resolver.TypeEnvironment;
            var pipeline = new Optimization[]
            {
                //   * Expand LINQ queries.
                new ExpandLinq(typeSystem.Boolean, typeSystem.Int32),

                //   * Inline direct method calls and devirtualize calls.
                Inlining.Instance,
                CopyPropagation.Instance,
                CallDevirtualization.Instance,
                DeadValueElimination.Instance,

                //   * Box to alloca, aggregates to scalars, scalars to registers.
                //     Also throw in GVN.
                BoxToAlloca.Instance,
                CopyPropagation.Instance,
                PartialScalarReplacement.Instance,
                GlobalValueNumbering.Instance,
                CopyPropagation.Instance,
                DeadValueElimination.Instance,
                AllocaToRegister.Instance,

                //   * Optimize control flow.
                InstructionSimplification.Instance,
                new ConstantPropagation(),
                MemoryAccessElimination.Instance,
                DeadValueElimination.Instance,
                new JumpThreading(true),
                SwitchSimplification.Instance,
                DuplicateReturns.Instance,
                TailRecursionElimination.Instance,
                BlockFusion.Instance
            };

            // Create an on-demand optimizer, which will optimize methods
            // lazily.
            var optimizer = new OnDemandOptimizer(
                pipeline,
                method => GetInitialMethodBody(method, typeSystem));

            var tasks = new List<Task>();
            foreach (var method in GetAllMethods(assembly))
            {
                tasks.Add(UpdateMethodBodyAsync(method, optimizer, typeSystem, printIr));
            }
            return Task.WhenAll(tasks);
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

        private static IEnumerable<ClrMethodDefinition> GetAllMethods(ClrAssembly assembly)
        {
            return assembly.Definition.Modules
                .SelectMany(module => module.Types)
                .SelectMany(type => GetAllMethods(type, assembly));
        }

        private static IEnumerable<ClrMethodDefinition> GetAllMethods(
            Mono.Cecil.TypeDefinition typeDefinition,
            ClrAssembly parentAssembly)
        {
            return typeDefinition.Methods
                .Select(parentAssembly.Resolve)
                .Cast<ClrMethodDefinition>()
                .Concat(
                    typeDefinition.NestedTypes.SelectMany(
                        type => GetAllMethods(type, parentAssembly)));
        }

        private static async Task UpdateMethodBodyAsync(
            ClrMethodDefinition method,
            Optimizer optimizer,
            TypeEnvironment typeSystem,
            bool printIr)
        {
            var optBody = await optimizer.GetBodyAsync(method);
            if (optBody == null)
            {
                // Looks like the method either doesn't have a body or
                // we can't handle it for some reason.
                return;
            }

            // Lower the optimized method body.
            optBody = optBody.WithImplementation(
                optBody.Implementation.Transform(
                    JumpToEntryRemoval.Instance,
                    DeadBlockElimination.Instance,
                    new SwitchLowering(typeSystem),
                    CopyPropagation.Instance,
                    InstructionReordering.Instance,
                    FuseMemoryAccesses.Instance,
                    new LowerBox(typeSystem.Object.GetDefiningAssemblyOrNull()),
                    DeadValueElimination.Instance,
                    new JumpThreading(false),
                    LowerDelegates.Instance));

            if (printIr)
            {
                PrintIr(method, method.Body, optBody);
            }

            // Select CIL instructions for the optimized method body.
            var newCilBody = ClrMethodBodyEmitter.Compile(optBody, method.Definition, typeSystem);
            lock (method.Definition)
            {
                method.Definition.Body = newCilBody;
            }
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

        private static LogEntry MakeDiagnostic(LogEntry entry)
        {
            return DiagnosticExtractor
                .Transform(entry, "ilopt")
                .Map(WrapBox.WordWrap);
        }
    }
}
