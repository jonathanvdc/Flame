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
        public abstract bool IsConstructor { get; }
        public abstract IEnumerable<IMethod> BaseMethods { get; }

        public virtual IEnumerable<IGenericParameter> GenericParameters
        {
            get
            {
                var methodRef = GetMethodReference();
                return CecilTypeBase.ConvertGenericParameters(methodRef, methodRef.Resolve, this, Module);
            }
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

        public IEnumerable<IParameter> Parameters { get { return GetParameters(); } }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            return null;
        }

        #region CecilTypeMemberBase Implementation

        public override MemberReference GetMemberReference()
        {
            return GetMethodReference();
        }

        #endregion

        #region Static

        public static IMethod Create(MethodReference Method, CecilModule Module)
        {
            return Module.Convert(Method);
        }
        public static IMethod ImportCecil(System.Reflection.MethodInfo Method, ICecilMember CecilMember)
        {
            return ImportCecil(Method, CecilMember.Module);
        }
        public static IMethod ImportCecil(System.Reflection.ConstructorInfo Method, ICecilMember CecilMember)
        {
            return ImportCecil(Method, CecilMember.Module);
        }
        public static IMethod ImportCecil(System.Reflection.MethodInfo Method, CecilModule Module)
        {
            return Module.Convert(Module.Module.Import(Method));
        }
        public static IMethod ImportCecil(System.Reflection.ConstructorInfo Method, CecilModule Module)
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
            if (!(other is CecilMethodBase) || other.GetIsGenericInstance())
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
