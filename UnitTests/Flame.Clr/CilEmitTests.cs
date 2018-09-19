using System;
using Loyc.MiniTest;
using Flame.Clr;
using System.Linq;
using Flame;
using Mono.Cecil;
using Flame.Collections;
using Flame.Ir;
using System.Collections.Generic;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Pixie;
using Pixie.Markup;
using Flame.Clr.Analysis;
using Flame.Clr.Emit;
using Flame.Compiler.Analysis;
using System.Text;
using Mono.Cecil.Rocks;
using OpCodes = Mono.Cecil.Cil.OpCodes;
using VariableDefinition = Mono.Cecil.Cil.VariableDefinition;
using Flame.Compiler.Transforms;

namespace UnitTests.Flame.Clr
{
    /// <summary>
    /// Unit tests that ensure 'Flame.Clr' CIL emission works.
    /// </summary>
    [TestFixture]
    public class CilEmitTests
    {
        public CilEmitTests(ILog log)
        {
            this.log = log;
        }

        private ILog log;

        private ClrAssembly corlib = LocalTypeResolutionTests.Corlib;

        [Test]
        public void RoundtripReturnIntegerConstant()
        {
            RoundtripStaticMethodBody(
                corlib.Definition.MainModule.TypeSystem.Int32,
                EmptyArray<TypeReference>.Value,
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldc_I4, 42);
                    ilProc.Emit(OpCodes.Ret);
                },
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldc_I4_S, (sbyte)42);
                    ilProc.Emit(OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripReturnArgument()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ret);
                },
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripStloc()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type },
                new[] { int32Type },
                ilProc =>
                {
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Stloc_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripAdd()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type, int32Type },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Add);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                ilProc =>
                {
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Add);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripCgt()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type, int32Type },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Cgt);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                ilProc =>
                {
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Cgt);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripBrPop()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type, int32Type },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    var firstInstr = ilProc.Create(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    var secondInstr = ilProc.Create(Mono.Cecil.Cil.OpCodes.Pop);
                    var firstThunk = ilProc.Create(Mono.Cecil.Cil.OpCodes.Br, firstInstr);
                    var secondThunk = ilProc.Create(Mono.Cecil.Cil.OpCodes.Br, secondInstr);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Br, firstThunk);
                    ilProc.Append(firstInstr);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Br, secondThunk);
                    ilProc.Append(secondInstr);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                    ilProc.Append(firstThunk);
                    ilProc.Append(secondThunk);
                },
                ilProc =>
                {
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripBrtrue()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type, int32Type },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    var target = ilProc.Create(Mono.Cecil.Cil.OpCodes.Nop);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                    ilProc.Append(target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                ilProc =>
                {
                    var target = ilProc.Create(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Brfalse, target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                    ilProc.Append(target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripBge()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type, int32Type },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    var target = ilProc.Create(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Bge, target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_7);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                    ilProc.Append(target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                ilProc =>
                {
                    var target = ilProc.Create(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Bge, target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_7);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                    ilProc.Append(target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                });
        }

        /// <summary>
        /// Writes a CIL method body, analyzes it as Flame IR,
        /// emits that as CIL and checks that the outcome matches
        /// what we'd expect.
        /// </summary>
        /// <param name="returnType">
        /// The return type of the method body.
        /// </param>
        /// <param name="parameterTypes">
        /// The parameter types of the method body.
        /// </param>
        /// <param name="localTypes">
        /// The local variable types of the method body.
        /// </param>
        /// <param name="emitBody">
        /// A function that writes the method body.
        /// </param>
        /// <param name="emitOracle">
        /// A function that writes the expected method body.
        /// </param>
        private void RoundtripStaticMethodBody(
            TypeReference returnType,
            IReadOnlyList<TypeReference> parameterTypes,
            IReadOnlyList<TypeReference> localTypes,
            Action<Mono.Cecil.Cil.ILProcessor> emitBody,
            Action<Mono.Cecil.Cil.ILProcessor> emitOracle)
        {
            // Define a method.
            var methodDef = new MethodDefinition(
                "f",
                MethodAttributes.Public | MethodAttributes.Static,
                returnType);

            foreach (var type in parameterTypes)
            {
                methodDef.Parameters.Add(new ParameterDefinition(type));
                int index = methodDef.Parameters.Count - 1;
                methodDef.Parameters[index].Name = "param_" + index;
            }

            // Emit the source CIL.
            var cilBody = new Mono.Cecil.Cil.MethodBody(methodDef);

            foreach (var localType in localTypes)
            {
                cilBody.Variables.Add(new Mono.Cecil.Cil.VariableDefinition(localType));
            }

            emitBody(cilBody.GetILProcessor());
            cilBody.Optimize();

            // Analyze it as Flame IR.
            var irBody = ClrMethodBodyAnalyzer.Analyze(
                cilBody,
                new Parameter(TypeHelpers.BoxIfReferenceType(corlib.Resolve(returnType))),
                default(Parameter),
                parameterTypes
                    .Select((type, i) => new Parameter(TypeHelpers.BoxIfReferenceType(corlib.Resolve(type)), "param_" + i))
                    .ToArray(),
                corlib);

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
                DeadValueElimination.Apply(
                    CopyPropagation.Apply(
                        AllocaToRegister.Apply(irBody.Implementation))));

            // Turn Flame IR back into CIL.
            var emitter = new ClrMethodBodyEmitter(methodDef, irBody, corlib.Resolver.TypeEnvironment);
            var newCilBody = emitter.Compile();

            // Synthesize the expected CIL.
            var expectedCilBody = new Mono.Cecil.Cil.MethodBody(methodDef);
            emitOracle(expectedCilBody.GetILProcessor());
            expectedCilBody.Optimize();

            // Check that the resulting CIL matches the expected CIL.
            var actual = FormatMethodBody(newCilBody);
            var oracle = FormatMethodBody(expectedCilBody);
            if (actual.Trim() != oracle.Trim())
            {
                var encoder = new EncoderState();
                var encodedImpl = encoder.Encode(irBody.Implementation);

                var actualIr = Les2LanguageService.Value.Print(
                    encodedImpl,
                    options: new LNodePrinterOptions
                    {
                        IndentString = new string(' ', 4)
                    });

                log.Log(
                    new LogEntry(
                        Severity.Message,
                        "emitted CIL-oracle mismatch",
                        "round-tripped CIL does not match the oracle. CIL emit output:",
                        new Paragraph(new WrapBox(actual.Trim(), 0, -actual.Length)),
                        DecorationSpan.MakeBold("remark: Flame IR:"),
                        new Paragraph(new WrapBox(actualIr.Trim(), 0, -actualIr.Length))));
            }
            Assert.AreEqual(oracle, actual);
        }

        private string FormatMethodBody(Mono.Cecil.Cil.MethodBody body)
        {
            var builder = new StringBuilder();
            builder.AppendLine(
                "Locals: [ " + string.Join(
                    ", ",
                    body.Variables.Select(local => local.VariableType.ToString())) + " ]");

            foreach (var insn in body.Instructions)
            {
                builder.AppendLine(insn.ToString());
            }
            return builder.ToString();
        }
    }
}
