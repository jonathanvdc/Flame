using Flame.Cecil.Emit;
using Flame.Compiler;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilDelegateType : ICecilType, IMethod, IEquatable<IMethod>
    {
        public CecilDelegateType(ICecilType Type)
        {
            this.Type = Type;
            this.invMethod = new Lazy<IMethod>(() => this.GetMethods().Single(item => item.Name == "Invoke" && !item.IsStatic));
        }

        public ICecilType Type { get; private set; }
        private Lazy<IMethod> invMethod;
        public IMethod InvokeMethod { get { return invMethod.Value; } }

        public static IMethod GetInvokeMethod(IType Type)
        {
            if (Type is CecilDelegateType)
            {
                return ((CecilDelegateType)Type).InvokeMethod;
            }
            else
            {
                return Type.GetMethods().Single(item => item.Name == "Invoke" && !item.IsStatic);
            }
        }

        public static ICecilType Create(IType Type, ICodeGenerator CodeGenerator)
        {
            if (Type is CecilDelegateType)
            {
                return (CecilDelegateType)Type;
            }
            else
            {
                return CodeGenerator.GetModule().TypeSystem.GetCanonicalDelegate(MethodType.GetMethod(Type));
            }
        }

        #region IMethod Implementation

        public IMethod[] GetBaseMethods()
        {
            return new IMethod[] { };
        }

        public IParameter[] GetParameters()
        {
            return InvokeMethod.GetParameters();
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            return null;
        }

        public bool IsConstructor
        {
            get { return false; }
        }

        public IMethod MakeGenericMethod(IEnumerable<IType> TypeArguments)
        {
            return new CecilDelegateType((ICecilType)Type.MakeGenericType(TypeArguments));
        }

        public IMethod GetGenericDeclaration()
        {
            return new CecilDelegateType((ICecilType)Type.GetGenericDeclaration());
        }

        public IType ReturnType
        {
            get { return InvokeMethod.ReturnType; }
        }

        public IType DeclaringType
        {
            get { return null; }
        }

        public bool IsStatic
        {
            get { return true; }
        }

        public TypeReference GetTypeReference()
        {
            return Type.GetTypeReference();
        }

        public MemberReference GetMemberReference()
        {
            return Type.GetMemberReference();
        }

        public CecilModule Module
        {
            get { return Type.Module; }
        }

        public string FullName
        {
            get { return Type.FullName; }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Type.Attributes;
        }

        public string Name
        {
            get { return Type.Name; }
        }

        #endregion

        #region IType Implementation

        public IEnumerable<IType> GetGenericArguments()
        {
            return Type.GetGenericArguments();
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return Type.GenericParameters;
        }

        public IContainerType AsContainerType()
        {
            return null;
        }

        public INamespace DeclaringNamespace
        {
            get { return Type.DeclaringNamespace; }
        }

        public IType[] GetBaseTypes()
        {
            return Type.BaseTypes;
        }

        public IMethod[] GetConstructors()
        {
            return Type.GetConstructors();
        }

        public IBoundObject GetDefaultValue()
        {
            return Type.GetDefaultValue();
        }

        public IField[] GetFields()
        {
            return Type.GetFields();
        }

        IType IType.GetGenericDeclaration()
        {
            return (IType)GetGenericDeclaration();
        }

        public ITypeMember[] GetMembers()
        {
            return Type.GetMembers();
        }

        public IMethod[] GetMethods()
        {
            return Type.GetMethods();
        }

        public IProperty[] GetProperties()
        {
            return Type.Properties;
        }

        public bool IsContainerType
        {
            get { return false; }
        }

        public IArrayType MakeArrayType(int Rank)
        {
            return new CecilArrayType(this, Rank);
        }

        public IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            return (IType)MakeGenericMethod(TypeArguments);
        }

        public IPointerType MakePointerType(PointerKind PointerKind)
        {
            return new CecilPointerType(this, PointerKind);
        }

        public IVectorType MakeVectorType(int[] Dimensions)
        {
            return new CecilVectorType(this, Dimensions); 
        }

        public IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            return Type.ResolveTypeParameter(TypeParameter);
        }

        public IAssembly DeclaringAssembly
        {
            get { return Type.DeclaringAssembly; }
        }

        public IType[] GetTypes()
        {
            return Type.GetTypes();
        }

        #endregion

        #region Equality/Hashing/ToString

        public bool Equals(IMethod other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is CecilDelegateType)
            {
                return Type.Equals(((CecilDelegateType)other).Type);
            }
            else
            {
                return Type.Equals(other);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is IMethod)
            {
                return Equals((IMethod)obj);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public override string ToString()
        {
            return Type.ToString();
        }

        #endregion
    }
}
