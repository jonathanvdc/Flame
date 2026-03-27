using System;
using Loyc.MiniTest;
using Flame.Clr;
using Flame.TypeSystem;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Flame;

namespace UnitTests.Flame.Clr
{
    /// <summary>
    /// Unit tests that ensure 'Flame.Clr' type resolution works
    /// for intra-assembly references.
    /// </summary>
    [TestFixture]
    public class MemberResolutionTests
    {
        private ClrAssembly corlib = LocalTypeResolutionTests.Corlib;

        private const string StringBoxName = "System.String box*";
        private const string Int32Name = "System.Int32";
        private const string BooleanName = "System.Boolean";

        [Test]
        public void ResolveStringEmpty()
        {
            var ts = corlib.Definition.MainModule.TypeSystem;
            var emptyFieldRef = ts.String.Resolve().Fields.Single(f => f.Name == "Empty");

            var emptyField = corlib.Resolve(emptyFieldRef);
            Assert.IsNotNull(emptyField);
            Assert.AreEqual(emptyField.Name.ToString(), emptyFieldRef.Name);
            Assert.IsTrue(emptyField.IsStatic);
            Assert.AreEqual(emptyField.FieldType.FullName.ToString(), StringBoxName);
        }

        [Test]
        public void ResolveInt32MinValue()
        {
            var ts = corlib.Definition.MainModule.TypeSystem;
            var minValueRef = ts.Int32.Resolve().Fields.Single(f => f.Name == "MinValue");

            var minValue = corlib.Resolve(minValueRef);
            Assert.IsNotNull(minValue);
            Assert.AreEqual(minValue.Name.ToString(), minValueRef.Name);
            Assert.IsTrue(minValue.IsStatic);
            Assert.AreEqual(minValue.FieldType.FullName.ToString(), Int32Name);
        }

        [Test]
        public void ResolveInt32Parse()
        {
            var ts = corlib.Definition.MainModule.TypeSystem;
            var parseRef = ts.Int32
                .Resolve()
                .Methods
                .Single(m => m.Name == "Parse" && m.Parameters.Count == 1);

            var parse = corlib.Resolve(parseRef);
            Assert.IsNotNull(parse);
            Assert.AreEqual(parse.Name.ToString(), parseRef.Name);
            Assert.IsTrue(parse.IsStatic);
            Assert.AreEqual(parse.ReturnParameter.Type.FullName.ToString(), Int32Name);
            Assert.AreEqual(parse.GenericParameters.Count, 0);
            Assert.AreEqual(parse.Parameters.Count, 1);
            Assert.AreEqual(parse.Parameters[0].Type.FullName.ToString(), StringBoxName);
        }

        [Test]
        public void ResolveStringIsNullOrEmpty()
        {
            var ts = corlib.Definition.MainModule.TypeSystem;
            var isNullOrEmptyRef = ts.String
                .Resolve()
                .Methods
                .Single(m => m.Name == "IsNullOrEmpty");

            var isNullOrEmpty = corlib.Resolve(isNullOrEmptyRef);
            Assert.IsNotNull(isNullOrEmpty);
            Assert.AreEqual(isNullOrEmpty.Name.ToString(), isNullOrEmptyRef.Name);
            Assert.IsTrue(isNullOrEmpty.IsStatic);
            Assert.AreEqual(isNullOrEmpty.ReturnParameter.Type.FullName.ToString(), BooleanName);
            Assert.AreEqual(isNullOrEmpty.GenericParameters.Count, 0);
            Assert.AreEqual(isNullOrEmpty.Parameters.Count, 1);
            Assert.AreEqual(isNullOrEmpty.Parameters[0].Type.FullName.ToString(), StringBoxName);
        }

        [Test]
        public void ResolveStringGenericJoin()
        {
            var ts = corlib.Definition.MainModule.TypeSystem;
            var joinRef = ts.String
                .Resolve()
                .Methods
                .Single(m =>
                    m.Name == "Join"
                        && m.HasGenericParameters
                        && m.Parameters.Count == 2
                        && m.Parameters[0].ParameterType == ts.String);

            var join = corlib.Resolve(joinRef);
            Assert.IsNotNull(join);
            Assert.AreEqual(join.Name.ToString(), joinRef.Name);
            Assert.IsTrue(join.IsStatic);
            Assert.AreEqual(join.ReturnParameter.Type.FullName.ToString(), StringBoxName);
            Assert.AreEqual(join.GenericParameters.Count, 1);
            Assert.AreEqual(join.GenericParameters[0].Name.ToString(), "T");
            Assert.AreEqual(join.Parameters.Count, 2);
            Assert.AreEqual(join.Parameters[0].Type.FullName.ToString(), StringBoxName);
            Assert.AreEqual(
                join.Parameters[1].Type.FullName.ToString(),
                "System.Collections.Generic.IEnumerable`1<System.String.Join.T> box*");
        }

        [Test]
        public void ResolveInt32ToString()
        {
            var ts = corlib.Definition.MainModule.TypeSystem;
            var intToStringRef = ts.Int32
                .Resolve()
                .Methods
                .Single(m => m.Name == "ToString" && m.Parameters.Count == 0);

            var intToString = corlib.Resolve(intToStringRef);
            Assert.IsNotNull(intToString);

            // Check that the signature looks right.
            Assert.AreEqual(intToString.Name.ToString(), intToStringRef.Name);
            Assert.IsFalse(intToString.IsStatic);
            Assert.AreEqual(intToString.ReturnParameter.Type.FullName.ToString(), StringBoxName);
            Assert.AreEqual(intToString.GenericParameters.Count, 0);
            Assert.AreEqual(intToString.Parameters.Count, 0);

            // Check that the `Int32.ToString` overrides some other `ToString` method.
            Assert.AreEqual(intToString.BaseMethods.Count, 1);
            Assert.AreEqual(intToString.BaseMethods[0].Name, intToString.Name);
        }

        [Test]
        public void ResolveRuntimeTypeListBuilderToArray()
        {
            var runtimeTypeRef = corlib.Definition.MainModule.Types
                .Single(type => type.FullName == "System.RuntimeType");

            var listBuilderRef = runtimeTypeRef.NestedTypes
                .Single(type => type.Name == "ListBuilder`1");

            var fieldInfoRef = corlib.Definition.MainModule.Types
                .Single(type => type.FullName == "System.Reflection.FieldInfo");

            var toArrayDef = listBuilderRef.Methods
                .Single(method => method.Name == "ToArray" && method.Parameters.Count == 0);

            var listBuilderInstanceRef = new GenericInstanceType(listBuilderRef);
            listBuilderInstanceRef.GenericArguments.Add(fieldInfoRef);

            var toArrayRef = new MethodReference(toArrayDef.Name, toArrayDef.ReturnType, listBuilderInstanceRef)
            {
                HasThis = toArrayDef.HasThis,
                ExplicitThis = toArrayDef.ExplicitThis,
                CallingConvention = toArrayDef.CallingConvention
            };
            foreach (var parameter in toArrayDef.Parameters)
            {
                toArrayRef.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }
            foreach (var genericParameter in toArrayDef.GenericParameters)
            {
                toArrayRef.GenericParameters.Add(new GenericParameter(genericParameter.Name, toArrayRef));
            }

            var toArray = corlib.Resolve(toArrayRef);
            Assert.IsNotNull(toArray);
            Assert.AreEqual(toArray.Name.ToString(), "ToArray");
            Assert.AreEqual(toArray.Parameters.Count, 0);
            Assert.AreEqual(TypeHelpers.BoxIfReferenceType(corlib.Resolve(fieldInfoRef.MakeArrayType())), toArray.ReturnParameter.Type);
        }

        [Test]
        public void ResolveRuntimeTypeListBuilderOfRuntimeTypeToArray()
        {
            var runtimeTypeRef = corlib.Definition.MainModule.Types
                .Single(type => type.FullName == "System.RuntimeType");

            var listBuilderRef = runtimeTypeRef.NestedTypes
                .Single(type => type.Name == "ListBuilder`1");

            var toArrayDef = listBuilderRef.Methods
                .Single(method => method.Name == "ToArray" && method.Parameters.Count == 0);

            var listBuilderInstanceRef = new GenericInstanceType(listBuilderRef);
            listBuilderInstanceRef.GenericArguments.Add(runtimeTypeRef);

            var toArrayRef = CreateHostedMethodReference(toArrayDef, listBuilderInstanceRef);
            var toArray = corlib.Resolve(toArrayRef);

            Assert.IsNotNull(toArray);
            Assert.AreEqual(toArray.Name.ToString(), "ToArray");
            Assert.AreEqual(toArray.Parameters.Count, 0);
            Assert.AreEqual(TypeHelpers.BoxIfReferenceType(corlib.Resolve(runtimeTypeRef.MakeArrayType())), toArray.ReturnParameter.Type);
        }

        [Test]
        public void ResolveExplicitGenericInterfaceOverride()
        {
            var overridingMethodRef = corlib.Definition.MainModule.GetTypes()
                .SelectMany(type => type.Methods)
                .First(method =>
                    method.Overrides.Any(ov =>
                        ov.Name == "Create"
                        && ov.FullName.Contains("System.RuntimeType/IGenericCacheEntry`1")
                        && ov.FullName.Contains("System.Enum/EnumInfo`1")));

            var overridingMethod = corlib.Resolve(overridingMethodRef);

            Assert.IsNotNull(overridingMethod);
            Assert.AreEqual(1, overridingMethod.BaseMethods.Count);
            Assert.AreEqual("Create", overridingMethod.BaseMethods[0].Name.ToString());
            Assert.AreEqual(1, overridingMethod.BaseMethods[0].Parameters.Count);
            Assert.AreEqual("System.RuntimeType box*", overridingMethod.BaseMethods[0].Parameters[0].Type.FullName.ToString());
        }

        [Test]
        public void ResolveMethodWithFunctionPointerSignature()
        {
            var typeSystem = corlib.Resolver.TypeEnvironment;
            var functionPointerMethodRef = corlib.Definition.MainModule.GetTypes()
                .SelectMany(type => type.Methods)
                .First(method =>
                    method.ReturnType is FunctionPointerType
                    || method.Parameters.Any(param => param.ParameterType is FunctionPointerType));

            var resolvedMethod = corlib.Resolve(functionPointerMethodRef);
            Assert.IsNotNull(resolvedMethod);

            if (functionPointerMethodRef.ReturnType is FunctionPointerType)
            {
                Assert.AreEqual(typeSystem.NaturalInt, resolvedMethod.ReturnParameter.Type);
            }
            else
            {
                var functionPointerParamIndex = functionPointerMethodRef.Parameters
                    .Select((param, index) => new { param, index })
                    .First(pair => pair.param.ParameterType is FunctionPointerType)
                    .index;
                Assert.AreEqual(typeSystem.NaturalInt, resolvedMethod.Parameters[functionPointerParamIndex].Type);
            }
        }

        [Test]
        public void ResolveArrayLength()
        {
            var arrayRef = corlib.Definition.MainModule.Types
                .Single(type => type.FullName == "System.Array")
                .Resolve();

            var lengthPropRef = arrayRef.Properties
                .Single(prop => prop.Name == "Length");

            // Resolve 'System.Array.Length'.
            var lengthProp = corlib.Resolve(lengthPropRef);
            Assert.IsNotNull(lengthProp);

            // Check that the signature looks right.
            Assert.AreEqual(lengthProp.Name.ToString(), lengthPropRef.Name);
        }

        [Test]
        public void ResolveArrayLengthGet()
        {
            var arrayRef = corlib.Definition.MainModule.Types
                .Single(type => type.FullName == "System.Array")
                .Resolve();

            var lengthPropRef = arrayRef.Properties
                .Single(prop => prop.Name == "Length");

            var lengthGetterRef = lengthPropRef.GetMethod;

            // Resolve 'System.Array.Length'.
            var lengthGetter = corlib.Resolve(lengthGetterRef) as ClrAccessorDefinition;
            Assert.IsNotNull(lengthGetter);

            // Check that the signature looks right.
            Assert.AreEqual(lengthGetter.Name.ToString(), lengthGetterRef.Name);
            Assert.AreEqual(lengthGetter.Kind, AccessorKind.Get);
            Assert.IsTrue(lengthGetter.Kind.IsLegalAccessor(lengthGetter));
        }

        [Test]
        public void ResolveArrayLengthGetIndirectly()
        {
            var intRef = corlib.Definition.MainModule.TypeSystem.Int32;
            var intArrayRef = intRef.MakeArrayType();

            // Resolve 'int[]'.
            var intArray = corlib.Resolve(intArrayRef);
            Assert.IsNotNull(intArray);

            // Grab 'System.Array' as the base type of 'int[]'.
            Assert.GreaterOrEqual(intArray.BaseTypes.Count, 1);
            var arrayType = intArray.BaseTypes[0];

            // Obtain the 'Length' property.
            var lengthProp = arrayType.Properties
                .Single(prop => prop.Name.ToString() == "Length");

            // Check that there is exactly one 'get' accessor.
            Assert.IsNotNull(
                lengthProp.Accessors
                .SingleOrDefault(accessor => accessor.Kind == AccessorKind.Get));
        }
        private static MethodReference CreateHostedMethodReference(
            MethodDefinition methodDef,
            TypeReference declaringType)
        {
            var methodRef = new MethodReference(methodDef.Name, methodDef.ReturnType, declaringType)
            {
                HasThis = methodDef.HasThis,
                ExplicitThis = methodDef.ExplicitThis,
                CallingConvention = methodDef.CallingConvention
            };
            foreach (var parameter in methodDef.Parameters)
            {
                methodRef.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }
            foreach (var genericParameter in methodDef.GenericParameters)
            {
                methodRef.GenericParameters.Add(new GenericParameter(genericParameter.Name, methodRef));
            }
            return methodRef;
        }

    }
}
