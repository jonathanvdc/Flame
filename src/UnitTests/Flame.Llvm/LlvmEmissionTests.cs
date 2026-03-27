using System;
using System.Collections.Generic;
using System.Linq;
using Flame;
using Flame.Collections;
using Flame.Compiler;
using Flame.Constants;
using Flame.Llvm;
using Flame.TypeSystem;
using LLVMSharp.Interop;
using Loyc.MiniTest;
using UnitTests.Flame.Clr;

namespace UnitTests.Flame.Llvm
{
    [TestFixture]
    public sealed class LlvmEmissionTests
    {
        [Test]
        public void LlvmBackendLowersLocalMemoryOperations()
        {
            var typeSystem = LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment;
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var programType = new DescribedType(new SimpleName("Program").Qualify(), assembly);
            assembly.AddType(programType);

            var method = new DescribedBodyMethod(programType, new SimpleName("RoundTripLocal"), true, typeSystem.Int32);
            method.Body = LlvmBackendSupport.CreateLocalRoundTripBody(typeSystem.Int32, new IntegerConstant(42));
            programType.AddMethod(method);

            var compiledMethod = LlvmBackendSupport.CompileMethod(assembly, programType, method);

            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(compiledMethod, instruction => instruction.IsAAllocaInst.Pointer() != IntPtr.Zero));
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(compiledMethod, instruction => instruction.IsAStoreInst.Pointer() != IntPtr.Zero));
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(compiledMethod, instruction => instruction.IsALoadInst.Pointer() != IntPtr.Zero));
        }

        [Test]
        public void LlvmBackendEmitsStructGepForInstanceFieldAccess()
        {
            var typeSystem = LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment;
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var holderType = new DescribedType(new SimpleName("Holder").Qualify(), assembly);
            assembly.AddType(holderType);

            var valueField = new DescribedField(holderType, new SimpleName("Value"), false, typeSystem.Int32);
            holderType.AddField(valueField);

            var method = new DescribedBodyMethod(holderType, new SimpleName("GetValue"), false, typeSystem.Int32);
            method.Body = LlvmBackendSupport.CreateFieldLoadBody(holderType, valueField, typeSystem.Int32);
            holderType.AddMethod(method);

            var compiledMethod = LlvmBackendSupport.CompileMethod(assembly, holderType, method);

            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(compiledMethod, instruction => instruction.InstructionOpcode == LLVMOpcode.LLVMGetElementPtr));
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(compiledMethod, instruction => instruction.IsALoadInst.Pointer() != IntPtr.Zero));
        }

        [Test]
        public void LlvmBackendEmitsDirectCallForStaticMethod()
        {
            var typeSystem = LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment;
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var programType = new DescribedType(new SimpleName("Program").Qualify(), assembly);
            assembly.AddType(programType);

            var callee = new DescribedBodyMethod(programType, new SimpleName("Callee"), true, typeSystem.Int32);
            callee.Body = LlvmBackendSupport.CreateConstantBody(typeSystem.Int32, new IntegerConstant(9));
            programType.AddMethod(callee);

            var caller = new DescribedBodyMethod(programType, new SimpleName("Caller"), true, typeSystem.Int32);
            caller.Body = LlvmBackendSupport.CreateStaticCallBody(callee);
            programType.AddMethod(caller);

            var compiledMethod = LlvmBackendSupport.CompileMethod(
                assembly,
                programType,
                caller,
                extraMembers: new[] { callee },
                extraBodies: new Dictionary<IMethod, MethodBody> { { callee, callee.Body } });

            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(compiledMethod, instruction => instruction.InstructionOpcode == LLVMOpcode.LLVMCall));
        }

        [Test]
        public void LlvmBackendEmitsDelegateThunkForStaticDelegate()
        {
            var typeSystem = LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment;
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var programType = new DescribedType(new SimpleName("Program").Qualify(), assembly);
            var delegateType = new DescribedType(new SimpleName("FakeDelegate").Qualify(), assembly);
            assembly.AddType(programType);
            assembly.AddType(delegateType);

            delegateType.AddField(new DescribedField(delegateType, new SimpleName("method_ptr"), false, typeSystem.NaturalInt));
            delegateType.AddField(new DescribedField(delegateType, new SimpleName("invoke_impl"), false, typeSystem.NaturalInt));
            delegateType.AddField(new DescribedField(delegateType, new SimpleName("m_target"), false, typeSystem.NaturalInt));

            var callee = new DescribedBodyMethod(programType, new SimpleName("Callee"), true, typeSystem.Int32);
            callee.AddParameter(new Parameter(typeSystem.Int32, "value"));
            callee.Body = LlvmBackendSupport.CreateIdentityBody(typeSystem.Int32);
            programType.AddMethod(callee);

            var caller = new DescribedBodyMethod(programType, new SimpleName("InvokeDelegate"), true, typeSystem.Int32);
            caller.AddParameter(new Parameter(typeSystem.Int32, "value"));
            caller.Body = LlvmBackendSupport.CreateDelegateCallBody(delegateType, callee, typeSystem.Int32);
            programType.AddMethod(caller);

            var moduleBuilder = LlvmBackendSupport.CompileModule(
                assembly,
                new global::Flame.IType[] { programType, delegateType },
                new ITypeMember[] { callee, caller }
                    .Concat(delegateType.Fields)
                    .ToArray(),
                new Dictionary<IMethod, MethodBody>
                {
                    { callee, callee.Body },
                    { caller, caller.Body }
                },
                null);

            var callerFunction = moduleBuilder.Module.GetNamedFunction(new ItaniumMangler(typeSystem).Mangle(caller, true));
            var thunk = moduleBuilder.Module.GetNamedFunction(new ItaniumMangler(typeSystem).Mangle(callee, true) + ".thunk");

            Assert.AreNotEqual(IntPtr.Zero, thunk.Pointer());
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(callerFunction, instruction => instruction.InstructionOpcode == LLVMOpcode.LLVMCall));
        }

        [Test]
        public void LlvmBackendRecognizesModernDelegateFieldLayout()
        {
            var typeSystem = LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment;
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var programType = new DescribedType(new SimpleName("Program").Qualify(), assembly);
            var delegateBaseType = new DescribedType(new SimpleName("DelegateBase").Qualify(), assembly);
            var delegateType = new DescribedType(new SimpleName("FakeDelegate").Qualify(), assembly);
            delegateType.AddBaseType(delegateBaseType);
            assembly.AddType(programType);
            assembly.AddType(delegateBaseType);
            assembly.AddType(delegateType);

            delegateBaseType.AddField(new DescribedField(delegateBaseType, new SimpleName("_target"), false, typeSystem.NaturalInt));
            delegateBaseType.AddField(new DescribedField(delegateBaseType, new SimpleName("_methodPtr"), false, typeSystem.NaturalInt));
            delegateBaseType.AddField(new DescribedField(delegateBaseType, new SimpleName("_methodPtrAux"), false, typeSystem.NaturalInt));

            var callee = new DescribedBodyMethod(programType, new SimpleName("Callee"), true, typeSystem.Int32);
            callee.AddParameter(new Parameter(typeSystem.Int32, "value"));
            callee.Body = LlvmBackendSupport.CreateIdentityBody(typeSystem.Int32);
            programType.AddMethod(callee);

            var caller = new DescribedBodyMethod(programType, new SimpleName("InvokeDelegate"), true, typeSystem.Int32);
            caller.AddParameter(new Parameter(typeSystem.Int32, "value"));
            caller.Body = LlvmBackendSupport.CreateDelegateCallBody(delegateType, callee, typeSystem.Int32);
            programType.AddMethod(caller);

            var moduleBuilder = LlvmBackendSupport.CompileModule(
                assembly,
                new global::Flame.IType[] { programType, delegateBaseType, delegateType },
                new ITypeMember[] { callee, caller }
                    .Concat(delegateBaseType.Fields)
                    .ToArray(),
                new Dictionary<IMethod, MethodBody>
                {
                    { callee, callee.Body },
                    { caller, caller.Body }
                },
                null);

            var callerFunction = moduleBuilder.Module.GetNamedFunction(new ItaniumMangler(typeSystem).Mangle(caller, true));
            var thunk = moduleBuilder.Module.GetNamedFunction(new ItaniumMangler(typeSystem).Mangle(callee, true) + ".thunk");

            Assert.AreNotEqual(IntPtr.Zero, thunk.Pointer());
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(callerFunction, instruction => instruction.InstructionOpcode == LLVMOpcode.LLVMCall));
        }

        [Test]
        public void LlvmBackendEmitsVirtualDispatchViaMetadataLoad()
        {
            var typeSystem = LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment;
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var baseType = new DescribedType(new SimpleName("Base").Qualify(), assembly);
            var derivedType = new DescribedType(new SimpleName("Derived").Qualify(), assembly);
            derivedType.AddBaseType(baseType);
            assembly.AddType(baseType);
            assembly.AddType(derivedType);

            var baseMethod = new DescribedBodyMethod(baseType, new SimpleName("GetValue"), false, typeSystem.Int32);
            baseMethod.AddAttribute(FlagAttribute.Virtual);
            baseMethod.Body = LlvmBackendSupport.CreateConstantBody(typeSystem.Int32, new IntegerConstant(1));
            baseType.AddMethod(baseMethod);

            var derivedCtor = new DescribedBodyMethod(derivedType, new SimpleName(".ctor"), false, typeSystem.Void);
            derivedCtor.IsConstructor = true;
            derivedCtor.Body = LlvmBackendSupport.CreateVoidBody(derivedType, typeSystem.Void);
            derivedType.AddMethod(derivedCtor);

            var derivedMethod = new DescribedBodyMethod(derivedType, new SimpleName("GetValue"), false, typeSystem.Int32);
            derivedMethod.AddAttribute(FlagAttribute.Virtual);
            derivedMethod.AddBaseMethod(baseMethod);
            derivedMethod.Body = LlvmBackendSupport.CreateConstantBody(typeSystem.Int32, new IntegerConstant(17));
            derivedType.AddMethod(derivedMethod);

            var caller = new DescribedBodyMethod(baseType, new SimpleName("CallVirtual"), true, typeSystem.Int32);
            caller.Body = LlvmBackendSupport.CreateVirtualCallBody(derivedCtor, baseMethod, baseType);
            baseType.AddMethod(caller);

            var moduleBuilder = LlvmBackendSupport.CompileModule(
                assembly,
                new global::Flame.IType[] { baseType, derivedType },
                new ITypeMember[] { baseMethod, derivedCtor, derivedMethod, caller },
                new Dictionary<IMethod, MethodBody>
                {
                    { baseMethod, baseMethod.Body },
                    { derivedCtor, derivedCtor.Body },
                    { derivedMethod, derivedMethod.Body },
                    { caller, caller.Body }
                },
                null);

            var mangler = new ItaniumMangler(typeSystem);
            var callerFunction = moduleBuilder.Module.GetNamedFunction(mangler.Mangle(caller, true));
            var vtable = moduleBuilder.Module.GetNamedGlobal("vtable_" + mangler.Mangle(derivedType, true));

            Assert.AreNotEqual(IntPtr.Zero, vtable.Pointer());
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(callerFunction, instruction => instruction.IsAGetElementPtrInst.Pointer() != IntPtr.Zero));
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(callerFunction, instruction => instruction.IsALoadInst.Pointer() != IntPtr.Zero));
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(callerFunction, instruction => instruction.InstructionOpcode == LLVMOpcode.LLVMCall));
        }

        [Test]
        public void LlvmBackendEmitsInterfaceDispatchThunk()
        {
            var typeSystem = LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment;
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var interfaceType = new DescribedType(new SimpleName("IValue").Qualify(), assembly);
            interfaceType.AddAttribute(FlagAttribute.InterfaceType);
            var implType = new DescribedType(new SimpleName("ValueImpl").Qualify(), assembly);
            implType.AddBaseType(interfaceType);
            assembly.AddType(interfaceType);
            assembly.AddType(implType);

            var interfaceMethod = new DescribedBodyMethod(interfaceType, new SimpleName("GetValue"), false, typeSystem.Int32);
            interfaceMethod.AddAttribute(FlagAttribute.Abstract);
            interfaceMethod.AddAttribute(FlagAttribute.Virtual);
            interfaceMethod.Body = LlvmBackendSupport.CreateConstantBody(typeSystem.Int32, new IntegerConstant(0));
            interfaceType.AddMethod(interfaceMethod);

            var implCtor = new DescribedBodyMethod(implType, new SimpleName(".ctor"), false, typeSystem.Void);
            implCtor.IsConstructor = true;
            implCtor.Body = LlvmBackendSupport.CreateVoidBody(implType, typeSystem.Void);
            implType.AddMethod(implCtor);

            var implMethod = new DescribedBodyMethod(implType, new SimpleName("GetValue"), false, typeSystem.Int32);
            implMethod.AddBaseMethod(interfaceMethod);
            implMethod.Body = LlvmBackendSupport.CreateConstantBody(typeSystem.Int32, new IntegerConstant(29));
            implType.AddMethod(implMethod);

            var caller = new DescribedBodyMethod(implType, new SimpleName("CallInterface"), true, typeSystem.Int32);
            caller.Body = LlvmBackendSupport.CreateVirtualCallBody(implCtor, interfaceMethod, interfaceType);
            implType.AddMethod(caller);

            var moduleBuilder = LlvmBackendSupport.CompileModule(
                assembly,
                new global::Flame.IType[] { interfaceType, implType },
                new ITypeMember[] { interfaceMethod, implCtor, implMethod, caller },
                new Dictionary<IMethod, MethodBody>
                {
                    { interfaceMethod, interfaceMethod.Body },
                    { implCtor, implCtor.Body },
                    { implMethod, implMethod.Body },
                    { caller, caller.Body }
                },
                null);

            var mangler = new ItaniumMangler(typeSystem);
            var callerFunction = moduleBuilder.Module.GetNamedFunction(mangler.Mangle(caller, true));
            var thunk = moduleBuilder.Module.GetNamedFunction(mangler.Mangle(interfaceMethod, true) + ".iface");

            Assert.AreNotEqual(IntPtr.Zero, thunk.Pointer());
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(thunk, instruction => instruction.IsASwitchInst.Pointer() != IntPtr.Zero));
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(callerFunction, instruction => instruction.InstructionOpcode == LLVMOpcode.LLVMCall));
        }

        [Test]
        public void LlvmBackendSynthesizesMainForEntryPoint()
        {
            var typeSystem = LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment;
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var programType = new DescribedType(new SimpleName("Program").Qualify(), assembly);
            assembly.AddType(programType);

            var entryPoint = new DescribedBodyMethod(programType, new SimpleName("Main"), true, typeSystem.Int32);
            entryPoint.Body = LlvmBackendSupport.CreateConstantBody(typeSystem.Int32, new IntegerConstant(123));
            programType.AddMethod(entryPoint);

            var moduleBuilder = LlvmBackendSupport.CompileModule(
                assembly,
                new[] { programType },
                new[] { entryPoint },
                new Dictionary<IMethod, MethodBody> { { entryPoint, entryPoint.Body } },
                entryPoint);
            var main = moduleBuilder.Module.GetNamedFunction("main");

            Assert.AreNotEqual(IntPtr.Zero, main.Pointer());
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(main, instruction => instruction.InstructionOpcode == LLVMOpcode.LLVMCall));
        }
    }
}
