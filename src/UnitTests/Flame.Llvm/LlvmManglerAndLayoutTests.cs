using System.Collections.Generic;
using System.Collections.Immutable;
using Flame;
using Flame.Compiler;
using Flame.Compiler.Pipeline;
using Flame.Constants;
using Flame.Llvm;
using Flame.Llvm.Emit;
using Flame.TypeSystem;
using LLVMSharp.Interop;
using Loyc.MiniTest;
using UnitTests.Flame.Clr;

namespace UnitTests.Flame.Llvm
{
    [TestFixture]
    public sealed class LlvmManglerAndLayoutTests
    {
        [Test]
        public void ItaniumManglerManglesNestedAndGenericTypes()
        {
            var testAssembly = new TestAssemblyContainer();
            var mangler = new ItaniumMangler(LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment);

            Assert.AreEqual("10SimpleType", mangler.Mangle(testAssembly.SimpleType, false));
            Assert.AreEqual("10SimpleType10NestedType", mangler.Mangle(testAssembly.NestedType, true));
            Assert.AreEqual("11GenericTypeIiE", mangler.Mangle(
                testAssembly.GenericType1.MakeGenericType(
                    LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment.Int32),
                true));
        }

        [Test]
        public void ModuleBuilderSkipsEmptyBaseTypesInImportedLayout()
        {
            var typeSystem = LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment;
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var baseType = new DescribedType(new SimpleName("Base").Qualify(), assembly);
            var derivedType = new DescribedType(new SimpleName("Derived").Qualify(), assembly);
            derivedType.AddBaseType(baseType);
            assembly.AddType(baseType);
            assembly.AddType(derivedType);

            var valueField = new DescribedField(derivedType, new SimpleName("Value"), false, typeSystem.Int32);
            derivedType.AddField(valueField);

            var moduleBuilder = new ModuleBuilder(
                LLVMModuleRef.CreateWithName("layout-test"),
                typeSystem,
                new ItaniumMangler(typeSystem),
                MallocInterface.Instance,
                new ClosedMetadataFormat(new[] { baseType, derivedType }, new ITypeMember[] { valueField }));

            var importedType = moduleBuilder.ImportType(derivedType);

            Assert.AreEqual(1u, importedType.CountStructElementTypesCompat());
            Assert.IsTrue(moduleBuilder.TryGetFieldIndex(valueField, out var fieldIndex));
            Assert.AreEqual(0, fieldIndex);
        }

        [Test]
        public void LlvmBackendCompileDefinesMangledMethod()
        {
            var typeSystem = LocalTypeResolutionTests.Corlib.Resolver.TypeEnvironment;
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var programType = new DescribedType(new SimpleName("Program").Qualify(), assembly);
            assembly.AddType(programType);

            var entryPoint = new DescribedBodyMethod(programType, new SimpleName("Main"), true, typeSystem.Int32);
            entryPoint.Body = LlvmBackendSupport.CreateConstantBody(typeSystem.Int32, new IntegerConstant(123));
            programType.AddMethod(entryPoint);

            var contents = new AssemblyContentDescription(
                assembly.FullName,
                AttributeMap.Empty,
                ImmutableHashSet.Create<global::Flame.IType>(programType),
                ImmutableHashSet.Create<ITypeMember>(entryPoint),
                new Dictionary<IMethod, MethodBody> { { entryPoint, entryPoint.Body } },
                null);

            var mangler = new ItaniumMangler(typeSystem);
            var moduleBuilder = LlvmBackend.Compile(contents, typeSystem, mangler);
            var compiledEntryPoint = moduleBuilder.Module.GetNamedFunction(mangler.Mangle(entryPoint, true));

            Assert.AreNotEqual(System.IntPtr.Zero, compiledEntryPoint.Pointer());
            Assert.AreEqual(mangler.Mangle(entryPoint, true), compiledEntryPoint.GetValueName());
        }
    }
}
