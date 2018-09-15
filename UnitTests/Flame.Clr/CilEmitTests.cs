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
                    var local = new VariableDefinition(int32Type);
                    ilProc.Body.Variables.Add(local);

                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Stloc_0);
                    ilProc.Emit(OpCodes.Ldloc_0);
                    ilProc.Emit(OpCodes.Ret);
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
                log.Log(
                    new LogEntry(
                        Severity.Message,
                        "emitted CIL-oracle mismatch",
                        "round-tripped CIL does not match the oracle. CIL emit output:"));
                // TODO: ugly hack to work around wrapping.
                Console.Error.WriteLine(actual.Trim());
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
