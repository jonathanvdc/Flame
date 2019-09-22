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
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Stloc_0);
                    ilProc.Emit(OpCodes.Ldloc_0);
                    ilProc.Emit(OpCodes.Ret);
                },
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripLdind()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type.MakePointerType() },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldobj, int32Type);
                    ilProc.Emit(OpCodes.Ret);
                },
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldind_I4);
                    ilProc.Emit(OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripVolatileLdind()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type.MakePointerType() },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Volatile);
                    ilProc.Emit(OpCodes.Ldobj, int32Type);
                    ilProc.Emit(OpCodes.Ret);
                },
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Volatile);
                    ilProc.Emit(OpCodes.Ldind_I4);
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
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Add);
                    ilProc.Emit(OpCodes.Ret);
                },
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Add);
                    ilProc.Emit(OpCodes.Ret);
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
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Cgt);
                    ilProc.Emit(OpCodes.Ret);
                },
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Cgt);
                    ilProc.Emit(OpCodes.Ret);
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
                    var firstInstr = ilProc.Create(OpCodes.Ldarg_0);
                    var secondInstr = ilProc.Create(OpCodes.Pop);
                    var firstThunk = ilProc.Create(OpCodes.Br, firstInstr);
                    var secondThunk = ilProc.Create(OpCodes.Br, secondInstr);
                    ilProc.Emit(OpCodes.Br, firstThunk);
                    ilProc.Append(firstInstr);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Br, secondThunk);
                    ilProc.Append(secondInstr);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(firstThunk);
                    ilProc.Append(secondThunk);
                },
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ret);
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
                    var target = ilProc.Create(OpCodes.Nop);
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Brtrue, target);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(target);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Ret);
                },
                ilProc =>
                {
                    var target = ilProc.Create(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Brfalse, target);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(target);
                    ilProc.Emit(OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripBrfalse()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type, int32Type },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    var target = ilProc.Create(OpCodes.Nop);
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Brfalse, target);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(target);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Ret);
                },
                ilProc =>
                {
                    var target = ilProc.Create(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Brfalse, target);
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(target);
                    ilProc.Emit(OpCodes.Ret);
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
                    var target = ilProc.Create(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Bge, target);
                    ilProc.Emit(OpCodes.Ldc_I4_7);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(target);
                    ilProc.Emit(OpCodes.Ret);
                },
                ilProc =>
                {
                    var target = ilProc.Create(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Bge, target);
                    ilProc.Emit(OpCodes.Ldc_I4_7);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(target);
                    ilProc.Emit(OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripCall()
        {
            var stringType = corlib.Definition.MainModule.TypeSystem.String;
            var concatMethod = stringType
                .Resolve()
                .Methods
                .First(method =>
                    method.Parameters.Count == 2
                    && method.Name == "Concat"
                    && method.Parameters[0].ParameterType == stringType
                    && method.Parameters[1].ParameterType == stringType);
            RoundtripStaticMethodBody(
                stringType,
                new[] { stringType, stringType },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Call, concatMethod);
                    ilProc.Emit(OpCodes.Ret);
                },
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldarg_1);
                    ilProc.Emit(OpCodes.Call, concatMethod);
                    ilProc.Emit(OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripJumpTableAsTestCascade()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    var one = ilProc.Create(OpCodes.Ldc_I4_1);
                    var two = ilProc.Create(OpCodes.Ldc_I4_2);
                    var four = ilProc.Create(OpCodes.Ldc_I4_4);
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Switch, new[] { one, two, four });
                    ilProc.Append(one);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(two);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(four);
                    ilProc.Emit(OpCodes.Ret);
                },
                ilProc =>
                {
                    var one = ilProc.Create(OpCodes.Ldc_I4_1);
                    var two = ilProc.Create(OpCodes.Ldc_I4_2);
                    var four = ilProc.Create(OpCodes.Ldc_I4_4);
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldc_I4_1);
                    ilProc.Emit(OpCodes.Beq_S, two);
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldc_I4_2);
                    ilProc.Emit(OpCodes.Beq_S, four);
                    ilProc.Append(one);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(four);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(two);
                    ilProc.Emit(OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripJumpTableAsSuch()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    var one = ilProc.Create(OpCodes.Ldc_I4_1);
                    var two = ilProc.Create(OpCodes.Ldc_I4_2);
                    var four = ilProc.Create(OpCodes.Ldc_I4_4);
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Switch, new[] { one, two, four, one, two, four });
                    ilProc.Append(one);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(two);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(four);
                    ilProc.Emit(OpCodes.Ret);
                },
                ilProc =>
                {
                    var one = ilProc.Create(OpCodes.Ldc_I4_1);
                    var two = ilProc.Create(OpCodes.Ldc_I4_2);
                    var four = ilProc.Create(OpCodes.Ldc_I4_4);
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldc_I4_1);
                    ilProc.Emit(OpCodes.Sub);
                    ilProc.Emit(OpCodes.Switch, new[] { two, four, one, two, four });
                    ilProc.Append(one);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(four);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(two);
                    ilProc.Emit(OpCodes.Ret);
                });
        }

        [Test]
        public void RoundtripJumpTableAsBitTests()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    var one = ilProc.Create(OpCodes.Ldc_I4_1);
                    var two = ilProc.Create(OpCodes.Ldc_I4_2);
                    var four = ilProc.Create(OpCodes.Ldc_I4_4);
                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(
                        OpCodes.Switch,
                        new[] { one, two, four, one, one, one, one, two, one, one, one, four, four, two });
                    ilProc.Append(one);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(two);
                    ilProc.Emit(OpCodes.Ret);
                    ilProc.Append(four);
                    ilProc.Emit(OpCodes.Ret);
                },
                @"
Locals: [ System.UInt32 ]
IL_0000: ldarg.0
IL_0001: ldc.i4.1
IL_0002: sub
IL_0003: dup
IL_0004: stloc.0
IL_0005: ldc.i4.s 12
IL_0007: ble.un.s IL_000b
IL_0009: ldc.i4.1
IL_000a: ret
IL_000b: ldc.i4.1
IL_000c: ldloc.0
IL_000d: shl
IL_000e: dup
IL_000f: stloc.0
IL_0010: ldc.i4 4161
IL_0015: and
IL_0016: brfalse.s IL_001a
IL_0018: ldc.i4.2
IL_0019: ret
IL_001a: ldloc.0
IL_001b: ldc.i4 3074
IL_0020: and
IL_0021: brfalse.s IL_0025
IL_0023: ldc.i4.4
IL_0024: ret
IL_0025: br.s IL_0009");
        }

        [Test]
        public void RoundBranchesAsJumpTable()
        {
            // IL_0000:  ldarg.0 
            // IL_0001:  ldc.i4.2 
            // IL_0002:  bne.un.s IL_0006

            // IL_0004:  ldc.i4.2 
            // IL_0005:  ret 
            // IL_0006:  ldarg.0 
            // IL_0007:  ldc.i4.3 
            // IL_0008:  bne.un.s IL_000c

            // IL_000a:  ldc.i4.4 
            // IL_000b:  ret 
            // IL_000c:  ldarg.0 
            // IL_000d:  ldc.i4.4 
            // IL_000e:  bne.un.s IL_0012

            // IL_0010:  ldc.i4.8 
            // IL_0011:  ret 
            // IL_0012:  ldarg.0 
            // IL_0013:  ldc.i4.5 
            // IL_0014:  bne.un.s IL_0019

            // IL_0016:  ldc.i4.s 0x10
            // IL_0018:  ret 
            // IL_0019:  ldarg.0 
            // IL_001a:  ldc.i4.6 
            // IL_001b:  bne.un.s IL_0020

            // IL_001d:  ldc.i4.s 0x20
            // IL_001f:  ret 
            // IL_0020:  ldc.i4.1 
            // IL_0021:  ret 

            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                new[] { int32Type },
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    var testThree = ilProc.Create(OpCodes.Ldarg_0);
                    var testFour = ilProc.Create(OpCodes.Ldarg_0);
                    var testFive = ilProc.Create(OpCodes.Ldarg_0);
                    var testSix = ilProc.Create(OpCodes.Ldarg_0);
                    var retOne = ilProc.Create(OpCodes.Ldc_I4_1);

                    ilProc.Emit(OpCodes.Ldarg_0);
                    ilProc.Emit(OpCodes.Ldc_I4_2);
                    ilProc.Emit(OpCodes.Bne_Un_S, testThree);
                    ilProc.Emit(OpCodes.Ldc_I4_2);
                    ilProc.Emit(OpCodes.Ret);

                    ilProc.Append(testThree);
                    ilProc.Emit(OpCodes.Ldc_I4_3);
                    ilProc.Emit(OpCodes.Bne_Un_S, testFour);
                    ilProc.Emit(OpCodes.Ldc_I4_4);
                    ilProc.Emit(OpCodes.Ret);

                    ilProc.Append(testFour);
                    ilProc.Emit(OpCodes.Ldc_I4_4);
                    ilProc.Emit(OpCodes.Bne_Un_S, testFive);
                    ilProc.Emit(OpCodes.Ldc_I4_8);
                    ilProc.Emit(OpCodes.Ret);

                    ilProc.Append(testFive);
                    ilProc.Emit(OpCodes.Ldc_I4_5);
                    ilProc.Emit(OpCodes.Bne_Un_S, testSix);
                    ilProc.Emit(OpCodes.Ldc_I4_S, (sbyte)16);
                    ilProc.Emit(OpCodes.Ret);

                    ilProc.Append(testSix);
                    ilProc.Emit(OpCodes.Ldc_I4_6);
                    ilProc.Emit(OpCodes.Bne_Un_S, retOne);
                    ilProc.Emit(OpCodes.Ldc_I4_S, (sbyte)32);
                    ilProc.Emit(OpCodes.Ret);

                    ilProc.Append(retOne);
                    ilProc.Emit(OpCodes.Ret);
                },
                @"
Locals: [  ]
IL_0000: ldarg.0
IL_0001: ldc.i4.2
IL_0002: sub
IL_0003: switch IL_0028,IL_0026,IL_0024,IL_0021,IL_001e
IL_001c: ldc.i4.1
IL_001d: ret
IL_001e: ldc.i4.s 32
IL_0020: ret
IL_0021: ldc.i4.s 16
IL_0023: ret
IL_0024: ldc.i4.8
IL_0025: ret
IL_0026: ldc.i4.4
IL_0027: ret
IL_0028: ldc.i4.2
IL_0029: ret");
        }

        [Test]
        public void FoldAddInt32()
        {
            var int32Type = corlib.Definition.MainModule.TypeSystem.Int32;
            RoundtripStaticMethodBody(
                int32Type,
                EmptyArray<TypeReference>.Value,
                EmptyArray<TypeReference>.Value,
                ilProc =>
                {
                    ilProc.Emit(OpCodes.Ldc_I4_2);
                    ilProc.Emit(OpCodes.Ldc_I4, 30);
                    ilProc.Emit(OpCodes.Add);
                    ilProc.Emit(OpCodes.Ret);
                },
                @"
Locals: [  ]
IL_0000: ldc.i4.s 32
IL_0002: ret");
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
            // Synthesize the expected CIL.
            var expectedCilBody = new Mono.Cecil.Cil.MethodBody(
                CreateStaticMethodDef(returnType, parameterTypes));
            emitOracle(expectedCilBody.GetILProcessor());
            expectedCilBody.Optimize();

            // Format the synthesized CIL.
            RoundtripStaticMethodBody(
                returnType,
                parameterTypes,
                localTypes,
                emitBody,
                FormatMethodBody(expectedCilBody));
        }

        private MethodDefinition CreateStaticMethodDef(
            TypeReference returnType,
            IReadOnlyList<TypeReference> parameterTypes)
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
            return methodDef;
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
        /// <param name="oracle">
        /// A printed version of the expected method body.
        /// </param>
        private void RoundtripStaticMethodBody(
            TypeReference returnType,
            IReadOnlyList<TypeReference> parameterTypes,
            IReadOnlyList<TypeReference> localTypes,
            Action<Mono.Cecil.Cil.ILProcessor> emitBody,
            string oracle)
        {
            // Define a method.
            var methodDef = CreateStaticMethodDef(returnType, parameterTypes);

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
                irBody.Implementation.Transform(
                    AllocaToRegister.Instance,
                    CopyPropagation.Instance,
                    new ConstantPropagation(),
                    SwitchSimplification.Instance,
                    DeadValueElimination.Instance,
                    new JumpThreading(true),
                    DeadBlockElimination.Instance,
                    new SwitchLowering(corlib.Resolver.TypeEnvironment),
                    CopyPropagation.Instance,
                    DeadValueElimination.Instance,
                    new JumpThreading(false)));

            // Turn Flame IR back into CIL.
            var newCilBody = ClrMethodBodyEmitter.Compile(irBody, methodDef, corlib.Resolver.TypeEnvironment);

            // Check that the resulting CIL matches the expected CIL.
            var actual = FormatMethodBody(newCilBody);
            actual = actual.Trim().Replace("\r", "");
            oracle = oracle.Trim().Replace("\r", "");
            if (actual != oracle)
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
                        new Paragraph(new WrapBox(actual, 0, -actual.Length)),
                        DecorationSpan.MakeBold("remark: Flame IR:"),
                        new Paragraph(new WrapBox(actualIr, 0, -actualIr.Length))));
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
