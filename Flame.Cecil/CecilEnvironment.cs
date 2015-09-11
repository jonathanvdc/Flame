using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Variables;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilEnvironment : IEnvironment //, IEnumerableEnvironment
    {
        public CecilEnvironment(CecilModule Module)
        {
            this.Module = Module;
        }

        public CecilModule Module { get; private set; }

        public string Name
        {
            get { return "CLR/Cecil"; }
        }

        public IType RootType
        {
            get { return Module.TypeSystem.Object; }
        }

        public IType EnumerableType
        {
            get { return Module.TypeSystem.Enumerable; }
        }

        public IType EnumeratorType
        {
            get { return Module.TypeSystem.Enumerator; }
        }

        /*private static void ForwardCall(IMethodBuilder Source, IMethod SourceMethod, IMethod TargetMethod)
        {
            var dict = new Dictionary<IType, IType>();
            foreach (var item in SourceMethod.DeclaringType.GenericParameters)
            {
                dict[item] = item;
            }
            dict[Source.DeclaringType] = ThisVariable.GetThisType(SourceMethod.DeclaringType);

            var converter = new TypeMappingConverter(dict);
            var methodConverter = new TypeMethodConverter(converter);

            var convTarget = methodConverter.Convert(TargetMethod);
            var convSource = methodConverter.Convert(SourceMethod);

            var bodyGen = Source.GetBodyGenerator();

            var invocation = bodyGen.EmitInvocation(convTarget,
                                                    bodyGen.GetThis().EmitGet(),
                                                    new ICodeBlock[] { });
            if (ConversionExpression.Instance.UseReinterpretCast(convTarget.ReturnType, convSource.ReturnType))
            {
                Source.SetMethodBody(bodyGen.EmitReturn(bodyGen.EmitTypeBinary(invocation, convSource.ReturnType, Operator.ReinterpretCast)));
            }
            else
            {
                Source.SetMethodBody(bodyGen.EmitReturn(invocation));
            }
        }

        public void ImplementEnumerable(ITypeBuilder TargetType, IType ElementType, IBodyMethod GetEnumeratorImplementation)
        {
            var baseType      = (ICecilType)EnumerableType.MakeGenericType(new IType[] { ElementType });
            var oldEnumerable = Module.ConvertStrict(typeof(System.Collections.IEnumerable));
            var oldEnumerator = Module.ConvertStrict(typeof(System.Collections.IEnumerator));
            var disposable    = Module.ConvertStrict(typeof(System.IDisposable));

            var cecilType = (CecilTypeBuilder)TargetType;

            var bTypes = cecilType.BaseTypes;
            if (!bTypes.Contains(baseType))
            {
                cecilType.DeclareBaseType(baseType);
            }
            if (!bTypes.Contains(oldEnumerable))
            {
                cecilType.DeclareBaseType(oldEnumerable);
            }
            if (!bTypes.Contains(disposable))
            {
                cecilType.DeclareBaseType(disposable);
            }

            // Implement IEnumerable<T>.GetEnumerator()

            var descGenericGetEnumeratorMethod = new DescribedMethod(GetEnumeratorImplementation.Name, TargetType, GetEnumeratorImplementation.ReturnType, GetEnumeratorImplementation.IsStatic);
            foreach (var item in GetEnumeratorImplementation.Attributes)
            {
                descGenericGetEnumeratorMethod.AddAttribute(item);
            }
            descGenericGetEnumeratorMethod.AddBaseMethod(baseType.GetMethod("GetEnumerator", false, descGenericGetEnumeratorMethod.ReturnType, new IType[] { }));
            var getEnumeratorMethod = TargetType.DeclareMethod(descGenericGetEnumeratorMethod);
            var getEnumeratorMethodBody = GetEnumeratorImplementation.GetMethodBody().Emit(getEnumeratorMethod.GetBodyGenerator());
            getEnumeratorMethod.SetMethodBody(getEnumeratorMethodBody);
            getEnumeratorMethod.Build();

            // Implement old IEnumerable.GetEnumerator()

            var descGetEnumeratorMethod = new DescribedMethod(typeof(System.Collections.IEnumerable).FullName + ".GetEnumerator", TargetType, oldEnumerator, false);
            descGetEnumeratorMethod.AddAttribute(new AccessAttribute(AccessModifier.Private));
            descGetEnumeratorMethod.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
            descGetEnumeratorMethod.AddBaseMethod(oldEnumerable.GetMethod("GetEnumerator", false, oldEnumerator, new IType[] { }));

            var oldGetEnumeratorMethod = TargetType.DeclareMethod(descGetEnumeratorMethod);
            ForwardCall(oldGetEnumeratorMethod, oldGetEnumeratorMethod, getEnumeratorMethod);
            oldGetEnumeratorMethod.Build();
        }

        public void ImplementEnumerator(ITypeBuilder TargetType, IType ElementType, IBodyMethod MoveNextImplementation, IProperty CurrentImplementation)
        {
            var baseType = (ICecilType)EnumeratorType.MakeGenericType(new IType[] { ElementType });
            var oldEnumerator = Module.ConvertStrict(typeof(System.Collections.IEnumerator));

            var cecilType = (CecilTypeBuilder)TargetType;

            var bTypes = cecilType.BaseTypes;
            if (!bTypes.Contains(baseType))
            {
                cecilType.DeclareBaseType(baseType);
            }
            if (!bTypes.Contains(oldEnumerator))
            {
                cecilType.DeclareBaseType(oldEnumerator);
            }

            // Implement IEnumerator.Reset()

            var descResetMethod = new DescribedMethod("Reset", TargetType, PrimitiveTypes.Void, false);
            descResetMethod.AddAttribute(new AccessAttribute(AccessModifier.Public));
            descResetMethod.AddBaseMethod(baseType.GetMethod("Reset", false, PrimitiveTypes.Void, new IType[] { }));

            var notImplementedCtor = cecilType.Module.ConvertStrict(typeof(NotImplementedException)).GetConstructor(new IType[] { }, false);

            var resetMethod = TargetType.DeclareMethod(descResetMethod);
            var resetBodyGen = (IExceptionCodeGenerator)resetMethod.GetBodyGenerator();
            var resetBody = resetBodyGen.EmitThrow(resetBodyGen.EmitInvocation(notImplementedCtor, null, new ICodeBlock[] { }));
            resetMethod.SetMethodBody(resetBody);
            resetMethod.Build();

            // Implement IEnumerator.MoveNext()

            var descMoveNextMethod = new DescribedMethod(MoveNextImplementation.Name, TargetType, MoveNextImplementation.ReturnType, MoveNextImplementation.IsStatic);
            foreach (var item in MoveNextImplementation.Attributes)
            {
                descMoveNextMethod.AddAttribute(item);
            }
            descMoveNextMethod.AddBaseMethod(baseType.GetMethod("MoveNext", false, descMoveNextMethod.ReturnType, new IType[] { }));
            var moveNextMethod = TargetType.DeclareMethod(descMoveNextMethod);
            var moveNextMethodBody = MoveNextImplementation.GetMethodBody().Emit(moveNextMethod.GetBodyGenerator());
            moveNextMethod.SetMethodBody(moveNextMethodBody);
            moveNextMethod.Build();

            // Implement IEnumerator<T>.Current

            var genericCurrentProperty = TargetType.DeclareProperty(CurrentImplementation);
            var getCurrentImpl = CurrentImplementation.GetGetAccessor();
            var descGetCurrentImpl = new DescribedAccessor(getCurrentImpl.AccessorType, genericCurrentProperty, getCurrentImpl.ReturnType);
            foreach (var item in getCurrentImpl.Attributes)
            {
                descGetCurrentImpl.AddAttribute(item);
            }
            descGetCurrentImpl.AddBaseMethod(baseType.Properties.GetProperty("Current", false).GetGetAccessor());
            var genericCurrentAccessor = genericCurrentProperty.DeclareAccessor(descGetCurrentImpl);
            var getCurrentMethodBody = ((IBodyMethod)getCurrentImpl).GetMethodBody().Emit(genericCurrentAccessor.GetBodyGenerator());
            genericCurrentAccessor.SetMethodBody(getCurrentMethodBody);
            genericCurrentAccessor.Build();
            genericCurrentProperty.Build();

            // Implement old System.Collections.IEnumerator.Current property

            var descOldCurrentProperty = new DescribedProperty(typeof(System.Collections.IEnumerator).FullName + ".Current", TargetType, RootType, false);
            descOldCurrentProperty.AddAttribute(new AccessAttribute(AccessModifier.Private));

            var oldCurrentProperty = TargetType.DeclareProperty(descOldCurrentProperty);

            var descOldCurrentGetter = new DescribedAccessor(AccessorType.GetAccessor, oldCurrentProperty, RootType);
            descOldCurrentGetter.IsStatic = false;
            descOldCurrentGetter.AddAttribute(new AccessAttribute(AccessModifier.Private));
            descOldCurrentGetter.AddAttribute(PrimitiveAttributes.Instance.ConstantAttribute);
            descOldCurrentGetter.AddBaseMethod(oldEnumerator.Properties.GetProperty("Current", false).GetGetAccessor());

            var oldCurrentGetter = oldCurrentProperty.DeclareAccessor(descOldCurrentGetter);
            ForwardCall(oldCurrentGetter, oldCurrentProperty.GetGetAccessor(), genericCurrentProperty.GetGetAccessor());
            oldCurrentGetter.Build();
            oldCurrentProperty.Build();

            // Implement IDisposable.Dispose()

            var descDisposeMethod = new DescribedMethod("Dispose", TargetType, PrimitiveTypes.Void, false);
            descDisposeMethod.AddAttribute(new AccessAttribute(AccessModifier.Public));
            descDisposeMethod.AddBaseMethod(baseType.GetMethod("Dispose", false, PrimitiveTypes.Void, new IType[] { }));

            var disposeMethod = TargetType.DeclareMethod(descDisposeMethod);
            disposeMethod.SetMethodBody(disposeMethod.GetBodyGenerator().EmitReturn(null));
            disposeMethod.Build();
        }*/
    }
}
