using System;
using Flame;
using Flame.Constants;
using Flame.Collections;
using Flame.Compiler;
using Flame.Compiler.Flow;
using Flame.Compiler.Instructions;
using Flame.Ir;
using Flame.TypeSystem;
using Loyc;
using Loyc.MiniTest;
using Loyc.Syntax;
using Loyc.Syntax.Les;
using Pixie;

namespace UnitTests.Flame.Ir
{
    [TestFixture]
    public class AssemblyCodecTest
    {
        public AssemblyCodecTest(ILog log)
        {
            this.log = log;
            this.decoder = new DecoderState(log, new TypeResolver().ReadOnlyView);
            this.encoder = new EncoderState();
        }

        private ILog log;

        private DecoderState decoder;

        private EncoderState encoder;

        [Test]
        public void RoundTripEmptyAssembly()
        {
            AssertRoundTripAssembly("#assembly(Test, { });");
        }

        [Test]
        public void RoundTripTypeAssemblyWithSingleType()
        {
            AssertRoundTripAssembly("#assembly(Test, { #type(A, #(), #(), { }); });");
        }

        [Test]
        public void RoundTripAssemblyWithMultipleTypes()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(A, #(), #(), { });
                    #type(B, #(), #(), { });
                });");
        }

        [Test]
        public void RoundTripAssemblyWithTypeRefs()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(A, #(), #(), { });
                    #type(B, #(), #(A), { });
                });");
        }

        [Test]
        public void RoundTripAssemblyWithNamespacedType()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(TestNamespace::A, #(), #(), { });
                });");
        }

        [Test]
        public void RoundTripAssemblyWithNestedType()
        {
            AssertRoundTripAssembly(@"
                #assembly(Test, {
                    #type(A, #(), #(), {
                        #type(B, #(), #(), { });
                    });
                });");
        }

        [Test]
        public void RoundTripAssemblyWithTypeParameter()
        {
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var typeA = new DescribedType(new SimpleName("A", 1).Qualify(), assembly);
            var typeAParam = new DescribedGenericParameter(typeA, "T");
            typeA.AddGenericParameter(typeAParam);
            assembly.AddType(typeA);
            Assert.IsNotNull(encoder.EncodeDefinition(assembly));
        }

        [Test]
        public void RoundTripAssemblyWithField()
        {
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var typeA = new DescribedType(new SimpleName("A").Qualify(), assembly);
            assembly.AddType(typeA);
            typeA.AddField(new DescribedField(typeA, new SimpleName("Instance"), true, typeA));
            AssertRoundTripAssembly(assembly);
        }

        [Test]
        public void RoundTripAssemblyWithNestedTypeAndField()
        {
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var typeA = new DescribedType(new SimpleName("A").Qualify(), assembly);
            assembly.AddType(typeA);
            typeA.AddNestedType(new DescribedType(new SimpleName("B"), typeA));
            typeA.AddField(new DescribedField(typeA, new SimpleName("Instance"), true, typeA));
            AssertRoundTripAssembly(assembly);
        }

        [Test]
        public void RoundTripAssemblyWithMethod()
        {
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var float64 = new DescribedType(new SimpleName("Float64").Qualify(), assembly);
            assembly.AddType(float64);
            float64.AddMethod(CreateConstantMethod(float64, "GetPi", false, new Float64Constant(3.1415)));
            AssertRoundTripAssembly(assembly);
        }

        [Test]
        public void RoundTripAssemblyWithParameterizedMethod()
        {
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var float64 = new DescribedType(new SimpleName("Float64").Qualify(), assembly);
            assembly.AddType(float64);

            var method = new DescribedBodyMethod(float64, new SimpleName("Id"), true, float64);
            method.AddParameter(new Parameter(float64, "value"));
            method.Body = CreateIdentityBody(float64, "value");
            float64.AddMethod(method);

            AssertRoundTripAssembly(assembly);
        }

        [Test]
        public void RoundTripFactorial()
        {
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var int32 = new DescribedType(new SimpleName("Int32").Qualify(), assembly);
            assembly.AddType(int32);

            var method = new DescribedBodyMethod(int32, new SimpleName("Factorial"), true, int32);
            method.AddParameter(new Parameter(int32, "value"));
            int32.AddMethod(method);

            var graph = new FlowGraphBuilder();
            var valueParam = graph.EntryPoint.AppendParameter(int32, "value");
            var recursiveCall = graph.EntryPoint.AppendInstruction(
                Instruction.CreateCall(method, MethodLookup.Static, new[] { valueParam.Tag }));
            graph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(int32, recursiveCall));
            method.Body = new MethodBody(
                new Parameter(int32),
                default(Parameter),
                new[] { new Parameter(int32, "value") },
                graph.ToImmutable());

            AssertRoundTripAssembly(assembly);
        }

        [Test]
        public void RoundTripFieldReference()
        {
            var assembly = new DescribedAssembly(new SimpleName("Test").Qualify());
            var int32 = new DescribedType(new SimpleName("Int32").Qualify(), assembly);
            assembly.AddType(int32);

            var predecessor = new DescribedField(int32, new SimpleName("Predecessor"), false, int32);
            int32.AddField(predecessor);

            var method = new DescribedBodyMethod(int32, new SimpleName("GetPredecessor"), false, int32);
            method.Body = CreateFieldLoadBody(int32, predecessor);
            int32.AddMethod(method);

            AssertRoundTripAssembly(assembly);
        }

        private void AssertRoundTripAssembly(string lesCode)
        {
            AssertRoundTripAssembly(StripTrivia(Les2LanguageService.Value.ParseSingle(lesCode)));
        }

        private void AssertRoundTripAssembly(LNode node)
        {
            var decoded = decoder.DecodeAssembly(node);
            Assert.AreEqual(
                node,
                StripTrivia(encoder.EncodeDefinition(decoded)));
        }

        private void AssertRoundTripAssembly(IAssembly assembly)
        {
            var encoded = encoder.EncodeDefinition(assembly);
            var decoded = decoder.DecodeAssembly(encoded);
            Assert.AreEqual(
                StripTrivia(encoded),
                StripTrivia(encoder.EncodeDefinition(decoded)));
        }

        private static DescribedBodyMethod CreateConstantMethod(
            IType parentType,
            string name,
            bool isStatic,
            Constant constant)
        {
            var method = new DescribedBodyMethod(parentType, new SimpleName(name), isStatic, parentType);
            var graph = new FlowGraphBuilder();
            var result = graph.EntryPoint.AppendInstruction(
                Instruction.CreateConstant(constant, parentType));
            graph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(parentType, result));
            method.Body = new MethodBody(
                new Parameter(parentType),
                default(Parameter),
                EmptyArray<Parameter>.Value,
                graph.ToImmutable());
            return method;
        }

        private static MethodBody CreateIdentityBody(IType type, string parameterName)
        {
            var graph = new FlowGraphBuilder();
            var value = graph.EntryPoint.AppendParameter(type, parameterName);
            graph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(type, value.Tag));
            return new MethodBody(
                new Parameter(type),
                default(Parameter),
                new[] { new Parameter(type, parameterName) },
                graph.ToImmutable());
        }

        private static MethodBody CreateFieldLoadBody(IType parentType, IField field)
        {
            var graph = new FlowGraphBuilder();
            var thisParam = graph.EntryPoint.AppendParameter(
                parentType.MakePointerType(PointerKind.Box),
                "this_ptr");
            var fieldPtr = graph.EntryPoint.AppendInstruction(
                Instruction.CreateGetFieldPointer(field, thisParam.Tag));
            var value = graph.EntryPoint.AppendInstruction(
                Instruction.CreateLoad(field.FieldType, fieldPtr));
            graph.EntryPoint.Flow = new ReturnFlow(Instruction.CreateCopy(field.FieldType, value));
            return new MethodBody(
                new Parameter(field.FieldType),
                new Parameter(parentType.MakePointerType(PointerKind.Box), "this_ptr"),
                EmptyArray<Parameter>.Value,
                graph.ToImmutable());
        }

        private LNode StripTrivia(LNode node)
        {
            var strippedNode = node.WithAttrs(
                attr => attr.IsTrivia ? Maybe<LNode>.NoValue : new Maybe<LNode>(attr));

            if (strippedNode.IsCall)
            {
                return strippedNode
                    .WithTarget(StripTrivia(strippedNode.Target))
                    .WithArgs(arg => new Maybe<LNode>(StripTrivia(arg)));
            }
            else
            {
                return strippedNode;
            }
        }
    }
}
