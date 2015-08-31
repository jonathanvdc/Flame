using Flame.Build;
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
        public abstract IMethod GetGenericDeclaration();
        public abstract bool IsConstructor { get; }
        public abstract IMethod[] GetBaseMethods();
        public abstract IEnumerable<IType> GetGenericArguments();

        public virtual IEnumerable<IGenericParameter> GetGenericParameters()
        {
            var methodRef = GetMethodReference();
            return CecilTypeBase.ConvertGenericParameters(methodRef, methodRef.Resolve, this, Module);
        }

        public virtual IType ReturnType
        {
            get
            {
                return Module.Convert(GetMethodReference().ReturnType);
            }
        }

        public virtual IParameter[] GetParameters()
        {
            return CecilParameter.GetParameters(this, GetMethodReference().Parameters);
        }

        public IParameter[] Parameters { get { return GetParameters(); } }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            return null;
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
                return Right.IsContainerType && Left.AsContainerType().ContainerKind == Right.AsContainerType().ContainerKind && ComparePossibleGenericParameters(Left.AsContainerType().ElementType, Right.AsContainerType().ElementType);
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

        public static ICecilMethod Create(MethodReference Method, CecilModule Module)
        {
            return Module.Convert(Method);
        }
        public static ICecilMethod ImportCecil(System.Reflection.MethodInfo Method, ICecilMember CecilMember)
        {
            return ImportCecil(Method, CecilMember.Module);
        }
        public static ICecilMethod ImportCecil(System.Reflection.ConstructorInfo Method, ICecilMember CecilMember)
        {
            return ImportCecil(Method, CecilMember.Module);
        }
        public static ICecilMethod ImportCecil(System.Reflection.MethodInfo Method, CecilModule Module)
        {
            return Module.Convert(Module.Module.Import(Method));
        }
        public static ICecilMethod ImportCecil(System.Reflection.ConstructorInfo Method, CecilModule Module)
        {
            return Module.Convert(Module.Module.Import(Method));
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
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }
            if (!(other is CecilMethodBase) || other.get_IsGenericInstance())
            {
                return false;
            }

            var thisRef = GetMethodReference();
            var otherRef = other.GetMethodReference();
            if (thisRef.Name == otherRef.Name && thisRef.GenericParameters.Count == otherRef.GenericParameters.Count && this.DeclaringType.Equals(other.DeclaringType))
            {
                var thisDef = thisRef.Resolve();
                var otherDef = otherRef.Resolve();
                return thisDef == otherDef;
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
