using System;
using Flame;
using Flame.Compiler;
using Flame.Constants;
using Flame.TypeSystem;
using LLVMSharp.Interop;
using NUnit.Framework;
using UnitTests.Flame.Clr;

namespace UnitTests.Flame.Llvm
{
    [TestFixture]
    [Category("LLVM")]
    public sealed class LlvmControlFlowTests
    {
        [Test]
        public void LlvmBackendEmitsPhiForBlockParameters()
        {
            var typeSystem = LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment;
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var programType = new DescribedType(new SimpleName("Program").Qualify(), assembly);
            assembly.AddType(programType);

            var method = new DescribedBodyMethod(programType, new SimpleName("Select"), true, typeSystem.Int32);
            method.AddParameter(new Parameter(typeSystem.Int32, "flag"));
            method.Body = LlvmBackendSupport.CreateSelectBody(typeSystem.Int32);
            programType.AddMethod(method);

            var compiledMethod = LlvmBackendSupport.CompileMethod(assembly, programType, method);

            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(compiledMethod, instruction => instruction.InstructionOpcode == LLVMOpcode.LLVMICmp));
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(compiledMethod, instruction => instruction.IsABranchInst.Pointer() != IntPtr.Zero));
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(compiledMethod, instruction => instruction.IsAPHINode.Pointer() != IntPtr.Zero));
        }

        [Test]
        public void LlvmBackendEmitsSwitchForIntegerSwitchFlow()
        {
            var typeSystem = LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment;
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var programType = new DescribedType(new SimpleName("Program").Qualify(), assembly);
            assembly.AddType(programType);

            var method = new DescribedBodyMethod(programType, new SimpleName("Dispatch"), true, typeSystem.Int32);
            method.AddParameter(new Parameter(typeSystem.Int32, "value"));
            method.Body = LlvmBackendSupport.CreateIntegerSwitchBody(typeSystem.Int32);
            programType.AddMethod(method);

            var compiledMethod = LlvmBackendSupport.CompileMethod(assembly, programType, method);

            Assert.IsTrue(compiledMethod.BasicBlocksCount >= 4);
            Assert.IsTrue(LlvmBackendSupport.ContainsInstruction(compiledMethod, instruction => instruction.IsASwitchInst.Pointer() != IntPtr.Zero));
        }
    }
}
