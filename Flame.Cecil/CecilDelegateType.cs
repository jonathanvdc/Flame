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
        public CecilDelegateType(IType Type, CecilModule Module)
        {
            this.Type = Type;
            this.invMethod = new Lazy<IMethod>(() => this.GetMethods().Single(item => item.Name == "Invoke" && !item.IsStatic));
        }

        public IType Type { get; private set; }
        private Lazy<IMethod> invMethod;
        public IMethod InvokeMethod { get { return invMethod.Value; } }

        public CecilModule Module { get; private set; }

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

        public static IType Create(IType Type, ICodeGenerator CodeGenerator)
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

        public IEnumerable<IMethod> BaseMethods
        {
            get { return new IMethod[] { }; }
        }

        public IEnumerable<IParameter> Parameters
        {
            get { return InvokeMethod.Parameters; }
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            return null;
        }

        public bool IsConstructor
        {
            get { return false; }
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
            return CecilTypeImporter.Import(Module, Type);
        }

        public MemberReference GetMemberReference()
        {
            return GetTypeReference();
        }

        public string FullName
        {
            get { return Type.FullName; }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return Type.Attributes; }
        }

        public string Name
        {
            get { return Type.Name; }
        }

        #endregion

        #region IType Implementation

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Type.GenericParameters; }
        }

        public INamespace DeclaringNamespace
        {
            get { return Type.DeclaringNamespace; }
        }

        public IEnumerable<IType> BaseTypes
        {
            get { return Type.BaseTypes; }
        }

        public IBoundObject GetDefaultValue()
        {
            return Type.GetDefaultValue();
        }

        public IEnumerable<IField> Fields
        {
            get { return Type.Fields; }
        }

        public IEnumerable<IMethod> Methods
        {
            get { return Type.Methods; }
        }

        public IEnumerable<IProperty> Properties
        {
            get { return Type.Properties; }
        }

        public IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            return TypeParameter;
        }

        public IAssembly DeclaringAssembly
        {
            get { return Type is INamespace ? ((INamespace)Type).DeclaringAssembly : Type.DeclaringNamespace.DeclaringAssembly; }
        }

        public IEnumerable<IType> Types
        {
            get { return Type is INamespace ? ((INamespace)Type).Types : Enumerable.Empty<IType>(); }
        }

        public IAncestryRules AncestryRules
        {
            get { return MethodTypeAncestryRules.Instance; }
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
