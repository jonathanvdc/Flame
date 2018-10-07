using System;
using System.IO;
using Flame.Clr;
using Flame.Clr.Emit;
using Flame.Compiler;
using Flame.Compiler.Analysis;
using Flame.Compiler.Transforms;
using Flame.TypeSystem;
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

            var outputPath = parsedOptions.GetValue<string>(Options.Output);
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = Path.GetFileNameWithoutExtension(inputPath) + ".opt" + Path.GetExtension(inputPath);
            }

            // Read the assembly from disk.
            Mono.Cecil.AssemblyDefinition cecilAsm;
            try
            {
                cecilAsm = Mono.Cecil.AssemblyDefinition.ReadAssembly(inputPath);
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
            OptimizeAssembly(cecilAsm, flameAsm, def => OptimizeBody(def, typeSystem));

            // Write the optimized assembly to disk.
            cecilAsm.Write(outputPath);

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
            TypeEnvironment typeSystem)
        {
            var irBody = method.Body;

            // Register analyses.
            irBody = new global::Flame.Compiler.MethodBody(
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
                    .WithAnalysis(ConservativeInstructionOrderingAnalysis.Instance));

            // Optimize the IR a tiny bit.
            irBody = irBody.WithImplementation(
                irBody.Implementation.Transform(
                    AllocaToRegister.Instance,
                    CopyPropagation.Instance,
                    new ConstantPropagation(),
                    SwitchSimplification.Instance,
                    DeadValueElimination.Instance,
                    new JumpThreading(true),
                    DeadBlockElimination.Instance,
                    new SwitchLowering(typeSystem),
                    CopyPropagation.Instance,
                    DeadValueElimination.Instance,
                    new JumpThreading(false)));

            return irBody;
        }
    }
}
