using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public abstract class CecilGenericTypeBase : ICecilType, IEquatable<CecilGenericTypeBase>
    {
        public CecilGenericTypeBase(ICecilType GenericDefinition)
        {
            this.GenericDefinition = GenericDefinition;
        }

        public ICecilType GenericDefinition { get; private set; }
        public CecilModule Module { get { return GenericDefinition.Module; } }

        public abstract IType ResolveTypeParameter(IGenericParameter TypeParameter);
        public abstract INamespace DeclaringNamespace { get; }
        public abstract TypeReference GetTypeReference();
        public abstract IEnumerable<IType> GetGenericArguments();
        public abstract string FullName { get; }
        public abstract string Name { get; }
        public abstract IType GetGenericDeclaration();

        public MemberReference GetMemberReference()
        {
            return GetTypeReference();
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return GenericDefinition.GetGenericParameters();
        }

        public IContainerType AsContainerType()
        {
            return this as IContainerType;
        }

        public IType[] GetBaseTypes()
        {
            return this.ResolveTypes(GenericDefinition.GetBaseTypes());
        }

        #region Members

        public ITypeMember[] GetMembers()
        {
            return CecilTypeBase.GetMembers(this);
        }

        public IMethod[] GetConstructors()
        {
            return this.GenericDefinition.GetConstructors().Select(item => new CecilGenericInstanceMethod(this, (ICecilMethod)item)).ToArray();
        }

        public IField[] GetFields()
        {
            return this.GenericDefinition.GetFields().Select(item => new CecilGenericInstanceField(this, (ICecilField)item)).ToArray();
        }

        public IMethod[] GetMethods()
        {
            return this.GenericDefinition.GetMethods().Select(item => new CecilGenericInstanceMethod(this, (ICecilMethod)item)).ToArray();
        }

        public IProperty[] GetProperties()
        {
            return this.GenericDefinition.GetProperties().Select(item => new CecilGenericInstanceProperty(this, (ICecilProperty)item)).ToArray();
        }

        public IType[] GetTypes()
        {
            IType[] oldTypes = GenericDefinition.GetTypes();
            IType[] newTypes = new IType[oldTypes.Length];
            for (int i = 0; i < newTypes.Length; i++)
            {
                newTypes[i] = new CecilGenericInstanceType(this, (ICecilType)oldTypes[i]);
            }
            return newTypes;
        }

        #endregion

        #region Attributes

        protected virtual IAttribute CompleteAttribute(IAttribute Attribute)
        {
            if (Attribute is EnumerableAttribute)
            {
                return new EnumerableAttribute(this.ResolveType(((EnumerableAttribute)Attribute).ElementType));
            }
            else
            {
                return Attribute;
            }
        }

        private IAttribute[] cachedAttrs;
        public IEnumerable<IAttribute> GetAttributes()
        {
            if (cachedAttrs == null)
            {
                cachedAttrs = GenericDefinition.GetAttributes().Select(CompleteAttribute).ToArray();
            }
            return cachedAttrs;
        }

        #endregion

        public virtual IBoundObject GetDefaultValue()
        {
            throw new NotImplementedException();
        }

        public bool IsContainerType
        {
            get { return this is IContainerType; }
        }

        public IArrayType MakeArrayType(int Rank)
        {
            return new CecilArrayType(this, Rank);
        }

        public IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            return new CecilGenericType(this, TypeArguments);
        }

        public IPointerType MakePointerType(PointerKind PointerKind)
        {
            return new CecilPointerType(this, PointerKind);
        }

        public IVectorType MakeVectorType(int[] Dimensions)
        {
            return new CecilVectorType(this, Dimensions);
        }

        public IAssembly DeclaringAssembly
        {
            get { return DeclaringNamespace.DeclaringAssembly; }
        }

        #region Equals/GetHashCode/ToString

        public override string ToString()
        {
            return FullName;
        }
        public override bool Equals(object obj)
        {
            if (obj is ICecilMember)
            {
                return Equals((ICecilMember)obj);
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return GenericDefinition.GetHashCode() ^ GetGenericArguments().Aggregate(0, (val, item) => val ^ item.GetHashCode());
        }
        public bool Equals(ICecilMember other)
        {
            if (other is ICecilType)
            {
                return Equals((ICecilType)other);
            }
            else
            {
                return GetMemberReference().Equals(other.GetMemberReference());
            }
        }
        public bool Equals(CecilGenericTypeBase other)
        {
            return GenericDefinition.Equals(other.GenericDefinition) && GetGenericArguments().SequenceEqual(other.GetGenericArguments());
        }
        public bool Equals(ICecilType other)
        {
            if (other is CecilGenericTypeBase)
            {
                return Equals((CecilGenericTypeBase)other);
            }
            else if (DeclaringNamespace.Equals(other.DeclaringNamespace) && this.Name == other.Name)
            {
                return GetGenericArguments().SequenceEqual(other.GetGenericArguments());
            }
            else
            {
                return false;
            }
        }

        #endregion
    }
}
