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
            this.cachedBaseTypes = new Lazy<IType[]>(GetBaseTypesCore);
            this.InitializeCache();
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
            return GenericDefinition.GenericParameters;
        }

        public IContainerType AsContainerType()
        {
            return this as IContainerType;
        }

        private Lazy<IType[]> cachedBaseTypes;

        private IType[] GetBaseTypesCore()
        {
            return this.ResolveTypes(GenericDefinition.BaseTypes);
        }

        public IType[] GetBaseTypes()
        {
            return cachedBaseTypes.Value;
        }

        #region Members

        private Lazy<IMethod[]> cachedCtors;
        private Lazy<IField[]> cachedFields;
        private Lazy<IMethod[]> cachedMethods;
        private Lazy<IProperty[]> cachedProperties;
        private Lazy<IType[]> cachedTypes;

        private void InitializeCache()
        {
            this.cachedCtors = new Lazy<IMethod[]>(GetConstructorsCore);
            this.cachedFields = new Lazy<IField[]>(GetFieldsCore);
            this.cachedMethods = new Lazy<IMethod[]>(GetMethodsCore);
            this.cachedProperties = new Lazy<IProperty[]>(GetPropertiesCore);
            this.cachedTypes = new Lazy<IType[]>(GetTypesCore);
        }

        public ITypeMember[] GetMembers()
        {
            return CecilTypeBase.GetMembers(this);
        }

        public IMethod[] GetConstructors()
        {
            return cachedCtors.Value;
        }

        public IField[] GetFields()
        {
            return cachedFields.Value;
        }

        public IMethod[] GetMethods()
        {
            return cachedMethods.Value;
        }

        public IProperty[] GetProperties()
        {
            return cachedProperties.Value;
        }

        public IType[] GetTypes()
        {
            return cachedTypes.Value;
        }

        private IMethod[] GetConstructorsCore()
        {
            return this.GenericDefinition.GetConstructors().Select(item => new CecilGenericInstanceMethod(this, (ICecilMethod)item)).ToArray();
        }

        private IField[] GetFieldsCore()
        {
            return this.GenericDefinition.GetFields().Select(item => new CecilGenericInstanceField(this, (ICecilField)item)).ToArray();
        }

        private IMethod[] GetMethodsCore()
        {
            return this.GenericDefinition.GetMethods().Select(item => new CecilGenericInstanceMethod(this, (ICecilMethod)item)).ToArray();
        }

        private IProperty[] GetPropertiesCore()
        {
            return this.GenericDefinition.Properties.Select(item => new CecilGenericInstanceProperty(this, (ICecilProperty)item)).ToArray();
        }

        private IType[] GetTypesCore()
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
                cachedAttrs = GenericDefinition.Attributes.Select(CompleteAttribute).ToArray();
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
            return GenericDefinition.GetHashCode() ^ (GetGenericArguments().Aggregate(0, (val, item) => val ^ item.GetHashCode()) << 16);
        }
        public bool Equals(ICecilMember other)
        {
            if (other is ICecilType)
            {
                return Equals((ICecilType)other);
            }
            else
            {
                return false;
            }
        }
        public bool Equals(CecilGenericTypeBase other)
        {
            return GenericDefinition.Equals(other.GenericDefinition) && this.GetAllGenericArguments().SequenceEqual(other.GetAllGenericArguments());
        }
        public bool Equals(ICecilType other)
        {
            if (other is CecilGenericTypeBase)
            {
                return Equals((CecilGenericTypeBase)other);
            }
            else
            {
                return false;
            }
        }

        #endregion
    }
}
