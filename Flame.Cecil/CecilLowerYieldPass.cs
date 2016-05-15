using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    /// <summary>
    /// A lower-yield pass for the CLR back-end.
    /// </summary>
    public class CecilLowerYieldPass : LowerYieldPassBase
    {
        private CecilLowerYieldPass() { }

        static CecilLowerYieldPass()
        {
            Instance = new CecilLowerYieldPass();
        }

        public static CecilLowerYieldPass Instance { get; private set; }

        public override void ImplementEnumerable(IEnvironment Environment, DescribedType TargetType, IType ElementType, IStatement GetEnumeratorBody)
        {
            var cecilEnv = (CecilEnvironment)Environment;
            var genericEnumerable = cecilEnv.EnumerableType.MakeGenericType(new IType[] { ElementType });
            var genericEnumerator = cecilEnv.EnumeratorType.MakeGenericType(new IType[] { ElementType });
            var oldEnumerable = cecilEnv.Module.ConvertStrict(typeof(System.Collections.IEnumerable));
            var oldEnumerator = cecilEnv.Module.ConvertStrict(typeof(System.Collections.IEnumerator));

            // Add the following base types: IEnumerable<T>, IEnumerable
            AddBaseTypes(TargetType, new IType[] { genericEnumerable, oldEnumerable });

            // Declare the following:
            //
            // public IEnumerable<T> GetEnumerator() : IEnumerable<T>.GetEnumerator
            // { stmt; }

            var descGenericGetEnumeratorMethod = new DescribedBodyMethod("GetEnumerator", TargetType, genericEnumerator, false);
            descGenericGetEnumeratorMethod.AddAttribute(new AccessAttribute(AccessModifier.Public));
            descGenericGetEnumeratorMethod.AddBaseMethod(
                genericEnumerable.Methods.GetMethod(new SimpleName("GetEnumerator"), false, descGenericGetEnumeratorMethod.ReturnType, new IType[] { }));
            descGenericGetEnumeratorMethod.Body = GetEnumeratorBody;
            TargetType.AddMethod(descGenericGetEnumeratorMethod);

            // Declare the following:
            //
            // private IEnumerator IEnumerable.GetEnumerator() : IEnumerable.GetEnumerator
            // { this.GetEnumerator(); }

            var descGetEnumeratorMethod = new DescribedBodyMethod(typeof(System.Collections.IEnumerable).FullName + ".GetEnumerator", TargetType, oldEnumerator, false);
            descGetEnumeratorMethod.AddAttribute(new AccessAttribute(AccessModifier.Private));
            descGetEnumeratorMethod.AddBaseMethod(
                oldEnumerable.Methods.GetMethod(new SimpleName("GetEnumerator"), false, oldEnumerator, new IType[] { }));
            ForwardParameterlessCall(descGetEnumeratorMethod, descGenericGetEnumeratorMethod);
            TargetType.AddMethod(descGetEnumeratorMethod);
        }

        public override void ImplementEnumerator(IEnvironment Environment, DescribedType TargetType, IType ElementType, IStatement MoveNextBody, IStatement GetCurrentBody)
        {
            var cecilEnv = (CecilEnvironment)Environment;
            var genericEnumerator = cecilEnv.EnumeratorType.MakeGenericType(new IType[] { ElementType });
            var oldEnumerator = cecilEnv.Module.ConvertStrict(typeof(System.Collections.IEnumerator));
            var disposable = cecilEnv.Module.ConvertStrict(typeof(System.IDisposable));

            // Add the following base types: IEnumerator<T>, IEnumerator, IDisposable
            AddBaseTypes(TargetType, new IType[] { genericEnumerator, oldEnumerator, disposable });

            // Implement IEnumerator.Reset()

            var descResetMethod = new DescribedBodyMethod("Reset", TargetType, PrimitiveTypes.Void, false);
            descResetMethod.AddAttribute(new AccessAttribute(AccessModifier.Public));
            descResetMethod.AddBaseMethod(
                genericEnumerator.GetMethod(new SimpleName("Reset"), false, PrimitiveTypes.Void, new IType[] { }));

            // Emit Reset's body: `throw new NotImplementedException();`

            var notImplementedCtor = cecilEnv.Module.ConvertStrict(typeof(NotImplementedException)).GetConstructor(new IType[] { }, false);
            descResetMethod.Body = new ThrowStatement(new NewObjectExpression(notImplementedCtor, new IExpression[] { }));
            TargetType.AddMethod(descResetMethod);

            // Implement IEnumerator.MoveNext()

            var descMoveNextMethod = new DescribedBodyMethod("MoveNext", TargetType, PrimitiveTypes.Boolean, false);
            descMoveNextMethod.AddAttribute(new AccessAttribute(AccessModifier.Public));
            descMoveNextMethod.AddBaseMethod(
                oldEnumerator.GetMethod(new SimpleName("MoveNext"), false, descMoveNextMethod.ReturnType, new IType[] { }));
            descMoveNextMethod.Body = MoveNextBody;
            TargetType.AddMethod(descMoveNextMethod);

            // Implement IEnumerator<T>.Current

            var descGenericCurrentGetProp = new DescribedProperty("Current", TargetType, ElementType, false);
            descGenericCurrentGetProp.AddAttribute(new AccessAttribute(AccessModifier.Public));
            var descGenericCurrentGetAcc = new DescribedBodyAccessor(AccessorType.GetAccessor, descGenericCurrentGetProp, ElementType);
            descGenericCurrentGetAcc.AddAttribute(new AccessAttribute(AccessModifier.Public));
            descGenericCurrentGetAcc.AddBaseMethod(
                genericEnumerator.Properties.GetProperty(new SimpleName("Current"), false).GetGetAccessor());
            descGenericCurrentGetAcc.Body = GetCurrentBody;
            descGenericCurrentGetProp.AddAccessor(descGenericCurrentGetAcc);
            TargetType.AddProperty(descGenericCurrentGetProp);

            // Implement old System.Collections.IEnumerator.Current property

            var descOldCurrentProperty = new DescribedProperty(typeof(System.Collections.IEnumerator).FullName + ".Current", TargetType, cecilEnv.RootType, false);
            descOldCurrentProperty.AddAttribute(new AccessAttribute(AccessModifier.Private));
            var descOldCurrentGetter = new DescribedBodyAccessor(AccessorType.GetAccessor, descOldCurrentProperty, cecilEnv.RootType);
            descOldCurrentGetter.IsStatic = false;
            descOldCurrentGetter.AddAttribute(new AccessAttribute(AccessModifier.Private));
            descOldCurrentGetter.AddBaseMethod(
                oldEnumerator.Properties.GetProperty(new SimpleName("Current"), false).GetGetAccessor());
            ForwardParameterlessCall(descOldCurrentGetter, descGenericCurrentGetAcc);
            descOldCurrentProperty.AddAccessor(descOldCurrentGetter);
            TargetType.AddProperty(descOldCurrentProperty);

            // Implement IDisposable.Dispose()

            var descDisposeMethod = new DescribedBodyMethod("Dispose", TargetType, PrimitiveTypes.Void, false);
            descDisposeMethod.AddAttribute(new AccessAttribute(AccessModifier.Public));
            descDisposeMethod.AddBaseMethod(
                disposable.GetMethod(new SimpleName("Dispose"), false, PrimitiveTypes.Void, new IType[] { }));
            descDisposeMethod.Body = new ReturnStatement();
            TargetType.AddMethod(descDisposeMethod);
        }

        private static void AddBaseTypes(DescribedType Target, IEnumerable<IType> BaseTypes)
        {
            foreach (var item in BaseTypes)
            {
                if (!Target.BaseTypes.Any(arg => arg.IsEquivalent(item)))
                {
                    Target.AddBaseType(item);
                }
            }
        }

        private static IMethod CreateAutoGenericMethod(IMethod Method)
        {
            if (Method.DeclaringType.GetIsGeneric() && Method.DeclaringType.GetIsGenericInstance())
            {
                var genDeclType = new GenericType(Method.DeclaringType, Method.DeclaringType.GenericParameters);
                return new GenericInstanceMethod(Method, genDeclType);
            }
            else
            {
                return Method;
            }
        }

        private static void ForwardParameterlessCall(DescribedBodyMethod Source, IMethod TargetMethod)
        {
            var genericTgt = CreateAutoGenericMethod(TargetMethod);
            var thisVar = ThisReferenceVariable.Instance.Create(Source.DeclaringType);

            IExpression callExpr = new InvocationExpression(genericTgt, thisVar.CreateGetExpression(), new IExpression[] { });
            if (ConversionExpression.Instance.UseReinterpretCast(callExpr.Type, Source.ReturnType))
            {
                callExpr = new ReinterpretCastExpression(callExpr, Source.ReturnType);
            }

            Source.Body = new ReturnStatement(callExpr);
        }
    }
}
