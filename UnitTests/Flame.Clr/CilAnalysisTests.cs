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

namespace UnitTests.Flame.Clr
{
    /// <summary>
    /// Unit tests that ensure 'Flame.Clr' CIL analysis works.
    /// </summary>
    [TestFixture]
    public class CilAnalysisTests
    {
        private ClrAssembly corlib = LocalTypeResolutionTests.Corlib;

        [Test]
        public void AnalyzeReturnIntegerConstant()
        {
            const string oracle = @"
{
    #entry_point(@entry-point, #(), { }, #goto(IL_0000()));
    #block(IL_0000, #(), {
        val_0 = const(42, System::Int32)();
    }, #return(copy(System::Int32)(val_0)));
};";

            AnalyzeStaticMethodBody(
                corlib.Definition.MainModule.TypeSystem.Int32,
                EmptyArray<TypeReference>.Value,
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4, 42);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                oracle);
        }

        [Test]
        public void AnalyzeReturnArgument()
        {
            const string oracle = @"
{
    #entry_point(@entry-point, #(#param(System::Int32, param_0)), {
        param_0_slot = alloca(System::Int32)();
        val_0 = store(System::Int32)(param_0_slot, param_0);
    }, #goto(IL_0000()));
    #block(IL_0000, #(), {
        val_1 = load(System::Int32)(param_0_slot);
    }, #return(copy(System::Int32)(val_1)));
};";

            AnalyzeStaticMethodBody(
                corlib.Definition.MainModule.TypeSystem.Int32,
                new[] { corlib.Definition.MainModule.TypeSystem.Int32 },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                oracle);
        }

        [Test]
        public void AnalyzeStloc()
        {
            const string oracle = @"
{
    #entry_point(@entry-point, #(#param(System::Int32, param_0)), {
        param_0_slot = alloca(System::Int32)();
        val_0 = store(System::Int32)(param_0_slot, param_0);
        local_0_slot = alloca(System::Int32)();
    }, #goto(IL_0000()));
    #block(IL_0000, #(), {
        val_1 = load(System::Int32)(param_0_slot);
        val_2 = store(System::Int32)(local_0_slot, val_1);
        val_3 = load(System::Int32)(local_0_slot);
    }, #return(copy(System::Int32)(val_3)));
};";

            AnalyzeStaticMethodBody(
                corlib.Definition.MainModule.TypeSystem.Int32,
                new[] { corlib.Definition.MainModule.TypeSystem.Int32 },
                new[] { corlib.Definition.MainModule.TypeSystem.Int32 },
                ilProc =>
                {
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Stloc_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                oracle);
        }

        [Test]
        public void AnalyzeAdd()
        {
            const string oracle = @"
{
    #entry_point(@entry-point, #(#param(System::Int32, param_0), #param(System::Int32, param_1)), {
        param_0_slot = alloca(System::Int32)();
        val_0 = store(System::Int32)(param_0_slot, param_0);
        param_1_slot = alloca(System::Int32)();
        val_1 = store(System::Int32)(param_1_slot, param_1);
    }, #goto(IL_0000()));
    #block(IL_0000, #(), {
        val_2 = load(System::Int32)(param_0_slot);
        val_3 = load(System::Int32)(param_1_slot);
        val_4 = intrinsic(@arith.add, System::Int32, #(System::Int32, System::Int32))(val_3, val_2);
    }, #return(copy(System::Int32)(val_4)));
};";

            AnalyzeStaticMethodBody(
                corlib.Definition.MainModule.TypeSystem.Int32,
                new[] {
                    corlib.Definition.MainModule.TypeSystem.Int32,
                    corlib.Definition.MainModule.TypeSystem.Int32
                },
                new TypeReference[] { },
                ilProc =>
                {
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Add);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                oracle);
        }

        [Test]
        public void AnalyzeCgt()
        {
            const string oracle = @"
{
    #entry_point(@entry-point, #(#param(System::Int32, param_0), #param(System::Int32, param_1)), {
        param_0_slot = alloca(System::Int32)();
        val_0 = store(System::Int32)(param_0_slot, param_0);
        param_1_slot = alloca(System::Int32)();
        val_1 = store(System::Int32)(param_1_slot, param_1);
    }, #goto(IL_0000()));
    #block(IL_0000, #(), {
        val_2 = load(System::Int32)(param_0_slot);
        val_3 = load(System::Int32)(param_1_slot);
        val_4 = intrinsic(@arith.gt, System::Boolean, #(System::Int32, System::Int32))(val_3, val_2);
        val_5 = intrinsic(@arith.convert, System::Int32, #(System::Boolean))(val_4);
    }, #return(copy(System::Int32)(val_5)));
};";

            AnalyzeStaticMethodBody(
                corlib.Definition.MainModule.TypeSystem.Int32,
                new[] {
                    corlib.Definition.MainModule.TypeSystem.Int32,
                    corlib.Definition.MainModule.TypeSystem.Int32
                },
                new TypeReference[] { },
                ilProc =>
                {
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Cgt);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                oracle);
        }

        /// <summary>
        /// Writes a CIL method body, analyzes it as Flame IR
        /// and checks that the result is what we'd expect.
        /// </summary>
        /// <param name="returnType">
        /// The return type of the method body.
        /// </param>
        /// <param name="parameterTypes">
        /// The parameter types of the method body.
        /// </param>
        /// <param name="emitBody">
        /// A function that writes the method body.
        /// </param>
        /// <param name="oracle">
        /// The expected Flame IR flow graph, as LESv2.
        /// </param>
        private void AnalyzeStaticMethodBody(
            TypeReference returnType,
            IReadOnlyList<TypeReference> parameterTypes,
            IReadOnlyList<TypeReference> localTypes,
            Action<Mono.Cecil.Cil.ILProcessor> emitBody,
            string oracle)
        {
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

            var cilBody = new Mono.Cecil.Cil.MethodBody(methodDef);

            foreach (var localType in localTypes)
            {
                cilBody.Variables.Add(new Mono.Cecil.Cil.VariableDefinition(localType));
            }

            emitBody(cilBody.GetILProcessor());

            var irBody = ClrMethodBodyAnalyzer.Analyze(
                cilBody,
                new Parameter(corlib.Resolve(returnType)),
                default(Parameter),
                parameterTypes
                    .Select((type, i) => new Parameter(corlib.Resolve(type), "param_" + i))
                    .ToArray(),
                corlib);

            var encoder = new EncoderState();
            var encodedImpl = encoder.Encode(irBody.Implementation);

            Assert.AreEqual(
                Les2LanguageService.Value.Print(
                    encodedImpl,
                    options: new LNodePrinterOptions
                    {
                        IndentString = new string(' ', 4)
                    }).Trim(),
                oracle.Trim());
        }
    }
}
