using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public abstract class CecilMethodBase : CecilTypeMemberBase, ICecilMethod, IEquatable<ICecilMethod>
    {
        public CecilMethodBase(ICecilType DeclaringType)
            : base(DeclaringType)
        {
        }

        public abstract MethodReference GetMethodReference();
        public abstract IEnumerable<IType> GetCecilGenericArguments();
        public abstract IMethod GetGenericDeclaration();
        public abstract bool IsConstructor { get; }
        public abstract bool IsComplete { get; }
        public abstract IMethod[] GetBaseMethods();

        public virtual IEnumerable<IType> GetGenericArguments()
        {
            return GetCecilGenericArguments();
        }

        public virtual IEnumerable<IGenericParameter> GetCecilGenericParameters()
        {
            var methodRef = GetMethodReference();
            return CecilTypeBase.ConvertGenericParameters(methodRef, methodRef.Resolve, this, AncestryGraph);
        }

        public virtual IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return GetCecilGenericParameters();
        }

        public IType ReturnType
        {
            get
            {
                return this.ResolveType(GetMethodReference().ReturnType);
            }
        }

        public IParameter[] GetParameters()
        {
            return CecilParameter.GetParameters(this, GetMethodReference().Parameters);
        }

        public IParameter[] Parameters { get { return GetParameters(); } }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            throw new NotImplementedException();
        }

        public virtual IMethod MakeGenericMethod(IEnumerable<IType> TypeArguments)
        {
            return new CecilGenericMethod(DeclaringType, this, TypeArguments);
        }

        #region CecilTypeMemberBase Implementation

        public override MemberReference GetMemberReference()
        {
            return GetMethodReference();
        }

        #endregion

        #region Static

        #region Method Selection

        private static bool CompareEnumerables<T>(IEnumerable<T> Left, IEnumerable<T> Right, Func<T, T, bool> Comparer)
        {
            var A = Left.GetEnumerator();
            var B = Right.GetEnumerator();
            bool goodA, goodB;
            while (true)
            {
                goodA = A.MoveNext();
                goodB = B.MoveNext();
                if (goodA && goodB)
                {
                    if (!Comparer(A.Current, B.Current))
                    {
                        return false;
                    }
                }
                else
                {
                    break;
                }
            }
            return goodA == goodB;
        }

        private static bool CompareEnumerables<T>(T[] Left, T[] Right, Func<T, T, bool> Comparer)
        {
            if (Left.Length != Right.Length)
            {
                return false;
            }
            for (int i = 0; i < Left.Length; i++)
            {
                if (!Comparer(Left[i], Right[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool ComparePossibleGenericParameters(IType Left, IType Right)
        {
            if (Left.get_IsGenericParameter())
            {
                if (Right.get_IsGenericParameter())
                {
                    return Left.Name == Right.Name;
                }
                else
                {
                    return false;
                }
            }
            else if (Left.IsContainerType)
            {
                return Right.IsContainerType && Left.AsContainerType().ContainerKind == Right.AsContainerType().ContainerKind && ComparePossibleGenericParameters(Left.AsContainerType().GetElementType(), Right.AsContainerType().GetElementType());
            }
            else if (Left.get_IsGenericInstance())
            {
                if (Right.GetGenericDeclaration().Equals(Left.GetGenericDeclaration()))
                {
                    var largs = Left.GetGenericArguments();
                    var rargs = Right.GetGenericArguments();
                    return CompareEnumerables(largs, rargs, ComparePossibleGenericParameters);
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return Left.Equals(Right);
            }
        }

        private static IMethod GetCorrespondingMethod(IEnumerable<IMethod> Candidates, IMethod Target)
        {
            string name = Target.Name;
            bool isStatic = Target.IsStatic;
            bool isGenericDecl = Target.get_IsGenericDeclaration();
            bool isGeneric = Target.get_IsGeneric();
            IType[] paramTypes = Target.GetParameters().GetTypes();
            IType retType = Target.ReturnType;
            foreach (var item in Candidates)
            {
                if (item.Name == name && item.IsStatic == isStatic)
                {
                    bool itemIsGenericDecl = item.get_IsGenericDeclaration();
                    if (itemIsGenericDecl == isGenericDecl)
                    {
                        var itemParamTypes = item.GetParameters().GetTypes();
                        bool success;
                        if (itemIsGenericDecl && isGeneric) // Generic and declaration
                        {
                            success = ComparePossibleGenericParameters(item.ReturnType, retType) && CompareEnumerables(paramTypes, itemParamTypes, ComparePossibleGenericParameters);
                        }
                        else
                        {
                            success = item.ReturnType.Equals(retType) && itemParamTypes.AreEqual(paramTypes);
                        }
                        if (success)
                        {
                            return item;
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        public static ICecilMethod Create(MethodReference Method)
        {
            return Create(CecilTypeBase.CreateCecil(Method.DeclaringType), Method);
        }
        public static ICecilMethod Create(ICecilType DeclaringType, MethodReference Method)
        {
            if (Method.IsGenericInstance)
            {
                return new CecilGenericMethod(DeclaringType, (GenericInstanceMethod)Method);
            }
            else
            {
                return new CecilMethod(DeclaringType, Method);
            }
        }
        public static ICecilMethod ImportCecil(System.Reflection.MethodInfo Method, ICecilMember CecilMember)
        {
            return ImportCecil(Method, CecilMember.GetMemberReference().Module);
        }
        public static ICecilMethod ImportCecil(System.Reflection.ConstructorInfo Method, ICecilMember CecilMember)
        {
            return ImportCecil(Method, CecilMember.GetMemberReference().Module);
        }
        public static ICecilMethod ImportCecil(System.Reflection.MethodInfo Method, ModuleDefinition Module)
        {
            return CecilMethodBase.Create(Module.Import(Method));
        }
        public static ICecilMethod ImportCecil(System.Reflection.ConstructorInfo Method, ModuleDefinition Module)
        {
            return CecilMethodBase.Create(Module.Import(Method));
        }
        public static ICecilMethod ImportCecil(IMethod Method, ICecilMember CecilMember)
        {
            return ImportCecil(Method, CecilMember.GetMemberReference().Module);
        }
        public static ICecilMethod ImportCecil(IMethod Method, ModuleDefinition Module)
        {
            if (Method.get_IsGenericInstance())
            {
                var genDecl = ImportCecil(Method.GetGenericDeclaration(), Module);
                if (genDecl != null)
                {
                    var typeArgs = Method.GetGenericArguments().Select((item) => CecilTypeBase.ImportCecil(item, Module)).ToArray();
                    return (ICecilMethod)genDecl.MakeGenericMethod(typeArgs);
                }
            }
            if (Method is ICecilMethod)
            {
                var cecilMethod = (ICecilMethod)Method;
                var methodRef = cecilMethod.GetMethodReference();
                if (methodRef.Module.Name == Module.Name)
                {
                    return cecilMethod;
                }
                else if (Method.DeclaringType is ICecilType)
                {
                    var cecilDeclType = (ICecilType)Method.DeclaringType;
                    if (cecilDeclType.GetModule().Name != Module.Name)
                    {
                        cecilDeclType = CecilTypeBase.ImportCecil(cecilDeclType, Module);
                        var tempMethod = new CecilMethod(cecilDeclType, methodRef);
                        var resultMethod = (ICecilMethod)GetCorrespondingMethod(cecilDeclType.GetMethods().Concat(cecilDeclType.GetConstructors()), tempMethod);
                        return Create(cecilDeclType, Module.Import(resultMethod.GetMethodReference(), cecilDeclType.GetTypeReference()));
                    }
                    else
                    {
                        return Create(cecilDeclType, Module.Import(methodRef, cecilDeclType.GetTypeReference()));
                    }
                }
                else
                {
                    return Create(Module.Import(methodRef));
                }
            }
            else if (Method.DeclaringType.get_IsPrimitive())
            {
                var declType = Method.DeclaringType;
                var type = CecilTypeBase.ImportCecil(declType, Module);
                if (Method is IAccessor)
                {
                    var declProp = ((IAccessor)Method).DeclaringProperty;
                    var propType = CecilTypeBase.Import(declProp.PropertyType, Module);
                    var indexerTypes = CecilTypeBase.Import(declProp.GetIndexerParameters().GetTypes(), Module);
                    var cecilProperties = type.GetProperties();
                    var cecilProp = cecilProperties.Single((item) =>
                    {
                        if (item.IsStatic == declProp.IsStatic && ((item.get_IsIndexer() && declProp.get_IsIndexer()) || item.Name == declProp.Name) && item.PropertyType.Equals(propType))
                        {
                            var indexerParams = item.GetIndexerParameterTypes();
                            return indexerTypes.AreEqual(indexerParams);
                        }
                        return false;
                    });
                    return (ICecilMethod)cecilProp.GetAccessor(((IAccessor)Method).AccessorType);
                }
                else
                {
                    return (ICecilMethod)type.GetMethod(Method.Name, Method.IsStatic, CecilTypeBase.Import(Method.ReturnType, Module), CecilTypeBase.Import(Method.GetParameters().GetTypes(), Module));
                }
            }
            else if (Method.Equals(PrimitiveMethods.Instance.Equals))
            {
                var objType = CecilTypeBase.Import<object>(Module);
                return (ICecilMethod)objType.GetMethod("Equals", false, PrimitiveTypes.Boolean, new IType[] { objType });
            }
            else if (Method.Equals(PrimitiveMethods.Instance.GetHashCode))
            {
                var objType = CecilTypeBase.ImportCecil<object>(Module);
                return (ICecilMethod)objType.GetMethod("GetHashCode", false, PrimitiveTypes.Int32, new IType[0]);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Equality

        public override bool Equals(object obj)
        {
            if (obj is ICecilMethod)
            {
                return Equals((ICecilMethod)obj);
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public virtual bool Equals(ICecilMethod other)
        {
            if (other.get_IsGenericInstance())
            {
                return false;
            }
            var thisRef = GetMethodReference();
            var otherRef = other.GetMethodReference();
            if (thisRef.GenericParameters.Count == otherRef.GenericParameters.Count && this.DeclaringType.Equals(other.DeclaringType))
            {
                var thisDef = thisRef.Resolve();
                var otherDef = otherRef.Resolve();
                return thisDef.Module.Name == otherDef.Module.Name && thisDef.MetadataToken == otherDef.MetadataToken;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return GetMethodReference().Resolve().GetHashCode();
        }

        #endregion
    }
}
