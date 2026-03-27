using System;
using Loyc;
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

namespace UnitTests.Flame.Clr
{
    /// <summary>
    /// Unit tests that ensure 'Flame.Clr' CIL analysis works.
    /// </summary>
    [TestFixture]
    public class CilAnalysisTests
    {
        public CilAnalysisTests(ILog log)
        {
            this.log = log;
        }

        private ILog log;

        private ClrAssembly corlib = LocalTypeResolutionTests.Corlib;

        [Test]
        public void AnalyzeReturnIntegerConstant()
        {
            const string oracle = @"
{
    #entry_point(@entry-point, #(), { }, #goto(IL_0000()));
    #block(IL_0000, #(), {
        IL_0000_val_0 = const(42, System::Int32)();
    }, #return(copy(System::Int32)(IL_0000_val_0)));
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
        IL_0000_val_0 = load(System::Int32)(param_0_slot);
    }, #return(copy(System::Int32)(IL_0000_val_0)));
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
        IL_0000_val_0 = load(System::Int32)(param_0_slot);
        IL_0000_val_1 = store(System::Int32)(local_0_slot, IL_0000_val_0);
        IL_0000_val_2 = load(System::Int32)(local_0_slot);
    }, #return(copy(System::Int32)(IL_0000_val_2)));
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
        IL_0000_val_0 = load(System::Int32)(param_0_slot);
        IL_0000_val_1 = load(System::Int32)(param_1_slot);
        IL_0000_val_2 = intrinsic(@arith.add, System::Int32, #(System::Int32, System::Int32))(IL_0000_val_0, IL_0000_val_1);
    }, #return(copy(System::Int32)(IL_0000_val_2)));
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
        IL_0000_val_0 = load(System::Int32)(param_0_slot);
        IL_0000_val_1 = load(System::Int32)(param_1_slot);
        IL_0000_val_2 = intrinsic(@arith.gt, System::Boolean, #(System::Int32, System::Int32))(IL_0000_val_0, IL_0000_val_1);
        IL_0000_val_3 = intrinsic(@arith.convert, System::Int32, #(System::Boolean))(IL_0000_val_2);
    }, #return(copy(System::Int32)(IL_0000_val_3)));
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

        [Test]
        public void AnalyzeCgt_Un()
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
        IL_0000_val_0 = load(System::Int32)(param_0_slot);
        IL_0000_val_1 = load(System::Int32)(param_1_slot);
        IL_0000_val_2 = intrinsic(@arith.convert, System::UInt32, #(System::Int32))(IL_0000_val_1);
        IL_0000_val_3 = intrinsic(@arith.convert, System::UInt32, #(System::Int32))(IL_0000_val_0);
        IL_0000_val_4 = intrinsic(@arith.gt, System::Boolean, #(System::UInt32, System::UInt32))(IL_0000_val_3, IL_0000_val_2);
        IL_0000_val_5 = intrinsic(@arith.convert, System::Int32, #(System::Boolean))(IL_0000_val_4);
    }, #return(copy(System::Int32)(IL_0000_val_5)));
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
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Cgt_Un);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                oracle);
        }

        [Test]
        public void AnalyzeBrPop()
        {
            const string oracle = @"
{
    #entry_point(@entry-point, #(#param(System::Int32, param_0), #param(System::Int32, param_1)), {
        param_0_slot = alloca(System::Int32)();
        val_0 = store(System::Int32)(param_0_slot, param_0);
        param_1_slot = alloca(System::Int32)();
        val_1 = store(System::Int32)(param_1_slot, param_1);
    }, #goto(IL_0000()));
    #block(IL_0000, #(), { }, #goto(block_0()));
    #block(block_0, #(), { }, #goto(block_1()));
    #block(block_1, #(), {
        IL_0000_val_0 = load(System::Int32)(param_0_slot);
        IL_0000_val_1 = load(System::Int32)(param_1_slot);
    }, #goto(block_2(IL_0000_val_0, IL_0000_val_1)));
    #block(block_2, #(#param(System::Int32, IL_0000_stackarg_0), #param(System::Int32, IL_0000_stackarg_1)), { }, #goto(block_3(IL_0000_stackarg_0, IL_0000_stackarg_1)));
    #block(block_3, #(#param(System::Int32, val_2), #param(System::Int32, val_3)), { }, #return(copy(System::Int32)(val_2)));
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
                oracle);
        }

        [Test]
        public void AnalyzeBrtrue()
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
        IL_0000_val_0 = load(System::Int32)(param_0_slot);
        IL_0000_val_1 = load(System::Int32)(param_1_slot);
    }, #switch(copy(System::Int32)(IL_0000_val_1), block_1(IL_0000_val_0), {
        #case(#(0), block_0(IL_0000_val_0));
    }));
    #block(block_1, #(#param(System::Int32, IL_0000_stackarg_0)), {
        val_2 = load(System::Int32)(param_1_slot);
    }, #return(copy(System::Int32)(val_2)));
    #block(block_0, #(#param(System::Int32, val_3)), { }, #return(copy(System::Int32)(val_3)));
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
                    var target = ilProc.Create(Mono.Cecil.Cil.OpCodes.Nop);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                    ilProc.Append(target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                oracle);
        }

        [Test]
        public void AnalyzeBrfalse()
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
        IL_0000_val_0 = load(System::Int32)(param_0_slot);
        IL_0000_val_1 = load(System::Int32)(param_1_slot);
    }, #switch(copy(System::Int32)(IL_0000_val_1), block_1(IL_0000_val_0), {
        #case(#(0), block_0(IL_0000_val_0));
    }));
    #block(block_0, #(#param(System::Int32, IL_0000_stackarg_0)), {
        val_2 = load(System::Int32)(param_1_slot);
    }, #return(copy(System::Int32)(val_2)));
    #block(block_1, #(#param(System::Int32, val_3)), { }, #return(copy(System::Int32)(val_3)));
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
                    var target = ilProc.Create(Mono.Cecil.Cil.OpCodes.Nop);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Brfalse, target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                    ilProc.Append(target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                oracle);
        }

        [Test]
        public void AnalyzeBge()
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
        IL_0000_val_0 = load(System::Int32)(param_0_slot);
        IL_0000_val_1 = load(System::Int32)(param_1_slot);
        IL_0000_val_2 = intrinsic(@arith.lt, System::Boolean, #(System::Int32, System::Int32))(IL_0000_val_0, IL_0000_val_1);
        IL_0000_val_3 = intrinsic(@arith.convert, System::Int32, #(System::Boolean))(IL_0000_val_2);
    }, #switch(copy(System::Int32)(IL_0000_val_3), block_1(), {
        #case(#(0), block_0());
    }));
    #block(block_0, #(), {
        val_2 = load(System::Int32)(param_1_slot);
    }, #return(copy(System::Int32)(val_2)));
    #block(block_1, #(), {
        val_3 = const(7, System::Int32)();
    }, #return(copy(System::Int32)(val_3)));
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
                    var target = ilProc.Create(Mono.Cecil.Cil.OpCodes.Nop);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Bge, target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_7);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                    ilProc.Append(target);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                oracle);
        }

        [Test]
        public void AnalyzeCall()
        {
            const string oracle = @"
{
    #entry_point(@entry-point, #(#param(#pointer(System::String, box), param_0), #param(#pointer(System::String, box), param_1)), {
        param_0_slot = alloca(#pointer(System::String, box))();
        val_0 = store(#pointer(System::String, box))(param_0_slot, param_0);
        param_1_slot = alloca(#pointer(System::String, box))();
        val_1 = store(#pointer(System::String, box))(param_1_slot, param_1);
    }, #goto(IL_0000()));
    #block(IL_0000, #(), {
        IL_0000_val_0 = load(#pointer(System::String, box))(param_0_slot);
        IL_0000_val_1 = load(#pointer(System::String, box))(param_1_slot);
        IL_0000_val_2 = call(@'::(System, String).Concat(#pointer(System::String, box), #pointer(System::String, box)) => #pointer(System::String, box), static)(IL_0000_val_0, IL_0000_val_1);
    }, #return(copy(#pointer(System::String, box))(IL_0000_val_2)));
};";
            var stringType = corlib.Definition.MainModule.TypeSystem.String;

            AnalyzeStaticMethodBody(
                stringType,
                new[] { stringType, stringType },
                new TypeReference[] { },
                ilProc =>
                {
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    ilProc.Emit(
                        Mono.Cecil.Cil.OpCodes.Call,
                        stringType
                            .Resolve()
                            .Methods
                            .First(method =>
                                method.Parameters.Count == 2
                                && method.Name == "Concat"
                                && method.Parameters[0].ParameterType == stringType
                                && method.Parameters[1].ParameterType == stringType));
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                oracle);
        }

        [Test]
        public void AnalyzeJumpTable()
        {
            const string oracle = @"
{
    #entry_point(@entry-point, #(#param(System::Int32, param_0)), {
        param_0_slot = alloca(System::Int32)();
        val_0 = store(System::Int32)(param_0_slot, param_0);
    }, #goto(IL_0000()));
    #block(IL_0000, #(), {
        IL_0000_val_0 = load(System::Int32)(param_0_slot);
    }, #switch(copy(System::Int32)(IL_0000_val_0), block_2(), {
        #case(#(1), block_0());
        #case(#(2), block_1());
    }));
    #block(block_2, #(), {
        val_1 = const(1, System::Int32)();
    }, #return(copy(System::Int32)(val_1)));
    #block(block_0, #(), {
        val_2 = const(2, System::Int32)();
    }, #return(copy(System::Int32)(val_2)));
    #block(block_1, #(), {
        val_3 = const(4, System::Int32)();
    }, #return(copy(System::Int32)(val_3)));
};";

            AnalyzeStaticMethodBody(
                corlib.Definition.MainModule.TypeSystem.Int32,
                new[] {
                    corlib.Definition.MainModule.TypeSystem.Int32
                },
                new TypeReference[] { },
                ilProc =>
                {
                    var one = ilProc.Create(Mono.Cecil.Cil.OpCodes.Ldc_I4_1);
                    var two = ilProc.Create(Mono.Cecil.Cil.OpCodes.Ldc_I4_2);
                    var four = ilProc.Create(Mono.Cecil.Cil.OpCodes.Ldc_I4_4);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Switch, new[] { one, two, four });
                    ilProc.Append(one);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                    ilProc.Append(two);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                    ilProc.Append(four);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
                },
                oracle);
        }

        [Test]
        public void AnalyzeIsinstOnValueType()
        {
            var methodDef = new MethodDefinition(
                "f",
                MethodAttributes.Public | MethodAttributes.Static,
                corlib.Definition.MainModule.TypeSystem.Int32);
            methodDef.Parameters.Add(new ParameterDefinition(corlib.Definition.MainModule.TypeSystem.Object));
            methodDef.Parameters[0].Name = "param_0";

            var cilBody = new Mono.Cecil.Cil.MethodBody(methodDef);
            var ilProc = cilBody.GetILProcessor();
            var returnFalse = ilProc.Create(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);

            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Isinst, corlib.Definition.MainModule.TypeSystem.UInt64);
            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Brfalse, returnFalse);
            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_1);
            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);
            ilProc.Append(returnFalse);
            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);

            var irBody = ClrMethodBodyAnalyzer.Analyze(
                cilBody,
                new Parameter(corlib.Resolve(corlib.Definition.MainModule.TypeSystem.Int32)),
                default(Parameter),
                new[]
                {
                    new Parameter(TypeHelpers.BoxIfReferenceType(corlib.Resolve(corlib.Definition.MainModule.TypeSystem.Object)), "param_0")
                },
                corlib);

            Assert.IsNotNull(irBody);
            Assert.AreEqual(0, irBody.Validate().Count);
        }

        [Test]
        public void AnalyzeFilterHandler()
        {
            var module = corlib.Definition.MainModule;
            var methodDef = new MethodDefinition(
                "f",
                MethodAttributes.Public | MethodAttributes.Static,
                module.TypeSystem.Int32);
            methodDef.Body.InitLocals = true;
            methodDef.Body.Variables.Add(new Mono.Cecil.Cil.VariableDefinition(module.TypeSystem.Int32));

            var cilBody = methodDef.Body;
            var ilProc = cilBody.GetILProcessor();
            var exceptionType = module.Types
                .Single(type => type.FullName == "System.Exception");
            var exceptionCtor = exceptionType.Methods
                .Single(method => method.IsConstructor && method.Parameters.Count == 0);

            var tryStart = ilProc.Create(Mono.Cecil.Cil.OpCodes.Newobj, exceptionCtor);
            var tryEnd = ilProc.Create(Mono.Cecil.Cil.OpCodes.Pop);
            var filterStart = ilProc.Create(Mono.Cecil.Cil.OpCodes.Pop);
            var handlerStart = ilProc.Create(Mono.Cecil.Cil.OpCodes.Pop);
            var end = ilProc.Create(Mono.Cecil.Cil.OpCodes.Ldloc_0);

            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Stloc_0);
            ilProc.Append(tryStart);
            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Throw);
            ilProc.Append(tryEnd);
            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_1);
            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Endfilter);
            ilProc.Append(handlerStart);
            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_1);
            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Stloc_0);
            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Leave, end);
            ilProc.Append(end);
            ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ret);

            cilBody.ExceptionHandlers.Add(
                new Mono.Cecil.Cil.ExceptionHandler(Mono.Cecil.Cil.ExceptionHandlerType.Filter)
                {
                    TryStart = tryStart,
                    TryEnd = tryEnd,
                    FilterStart = filterStart,
                    HandlerStart = handlerStart,
                    HandlerEnd = end
                });

            var irBody = ClrMethodBodyAnalyzer.Analyze(
                cilBody,
                new Parameter(corlib.Resolve(module.TypeSystem.Int32)),
                default(Parameter),
                new Parameter[0],
                corlib);

            Assert.IsNotNull(irBody);
            Assert.AreEqual(0, irBody.Validate().Count);
        }

        [Test]
        public void AnalyzeConv_R_Un()
        {
            const string oracle = @"
{
    #entry_point(@entry-point, #(#param(System::Int32, param_0)), {
        param_0_slot = alloca(System::Int32)();
        val_0 = store(System::Int32)(param_0_slot, param_0);
    }, #goto(IL_0000()));
    #block(IL_0000, #(), {
        IL_0000_val_0 = load(System::Int32)(param_0_slot);
        IL_0000_val_1 = intrinsic(@arith.convert, System::UInt32, #(System::Int32))(IL_0000_val_0);
        IL_0000_val_2 = intrinsic(@arith.convert, System::Double, #(System::UInt32))(IL_0000_val_1);
    }, #return(copy(System::Double)(IL_0000_val_2)));
};";

            AnalyzeStaticMethodBody(
                corlib.Definition.MainModule.TypeSystem.Double,
                new[] {
                    corlib.Definition.MainModule.TypeSystem.Int32
                },
                new TypeReference[] { },
                ilProc =>
                {
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    ilProc.Emit(Mono.Cecil.Cil.OpCodes.Conv_R_Un);
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
                new Parameter(TypeHelpers.BoxIfReferenceType(corlib.Resolve(returnType))),
                default(Parameter),
                parameterTypes
                    .Select((type, i) => new Parameter(TypeHelpers.BoxIfReferenceType(corlib.Resolve(type)), "param_" + i))
                    .ToArray(),
                corlib);

            var encoder = new EncoderState();
            var encodedImpl = encoder.Encode(irBody.Implementation);

            var actual = Les2LanguageService.Value.Print(
                encodedImpl,
                options: new LNodePrinterOptions
                {
                    IndentString = new string(' ', 4)
                });

            var expectedNode = NormalizeNode(Les2LanguageService.Value.ParseSingle(oracle));
            var actualNode = NormalizeNode(Les2LanguageService.Value.ParseSingle(actual));

            if (!actualNode.Equals(expectedNode))
            {
                log.Log(
                    new LogEntry(
                        Pixie.Severity.Message,
                        "CIL analysis-oracle mismatch",
                        "analyzed CIL does not match the oracle. CIL analysis output:"));
                // TODO: ugly hack to work around wrapping.
                Console.Error.WriteLine(actual.Trim());
            }

            Assert.AreEqual(
                actualNode,
                expectedNode);
        }

        private static LNode NormalizeNode(LNode node)
        {
            var strippedNode = node.WithAttrs(
                attr => attr.IsTrivia ? Maybe<LNode>.NoValue : new Maybe<LNode>(attr));

            if (strippedNode.IsCall)
            {
                return strippedNode
                    .WithTarget(NormalizeNode(strippedNode.Target))
                    .WithArgs(arg => new Maybe<LNode>(NormalizeNode(arg)));
            }
            else
            {
                return strippedNode;
            }
        }
    }
}
