using Flame.Build;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public abstract class CecilTypeBase : CecilMember, ICecilType, IEquatable<ICecilType>
    {
        public CecilTypeBase()
        {
            InitializeMembers();
        }
        public CecilTypeBase(AncestryGraph AncestryGraph)
            : base(AncestryGraph)
        {
            InitializeMembers();
        }

        #region ICecilType Implementation

        public abstract TypeReference GetTypeReference();
        public abstract IType ResolveTypeParameter(IGenericParameter TypeParameter);

        public override MemberReference GetMemberReference()
        {
            return GetTypeReference();
        }

        #endregion

        #region Abstract

        public abstract IType[] GetBaseTypes();
        public abstract IType GetGenericDeclaration();
        protected abstract override IEnumerable<IAttribute> GetMemberAttributes();
        protected abstract override IList<CustomAttribute> GetCustomAttributes();
        public abstract bool IsContainerType { get; }
        public abstract IContainerType AsContainerType();


        public abstract IBoundObject GetDefaultValue();

        protected abstract IList<MethodDefinition> GetCecilMethods();
        protected abstract IList<PropertyDefinition> GetCecilProperties();
        protected abstract IList<FieldDefinition> GetCecilFields();
        protected abstract IList<EventDefinition> GetCecilEvents();

        #endregion

        #region Properties

        public bool IsValueType
        {
            get
            {
                return this.get_IsValueType();
            }
        }

        public bool IsInterface
        {
            get
            {
                return this.get_IsInterface();
            }
        }

        #endregion

        #region Type Members

        public static ITypeMember[] GetMembers(ICecilType DeclaringType)
        {
            return DeclaringType.GetMethods().Concat<ITypeMember>(DeclaringType.GetConstructors()).Concat(DeclaringType.GetFields()).Concat(DeclaringType.GetProperties()).ToArray();
        }
        public static IMethod[] ConvertMethodDefinitions(ICecilType DeclaringType, IList<MethodDefinition> MethodDefinitions, bool IsConstructor)
        {
            List<IMethod> methods = new List<IMethod>();
            var declRef = DeclaringType.GetTypeReference();
            foreach (var item in MethodDefinitions)
            {
                if (item.IsConstructor == IsConstructor && (item.IsConstructor || !item.IsSpecialName))
                {
                    methods.Add(new CecilMethod(DeclaringType, item));
                }
            }
            return methods.ToArray();
        }
        public static IField[] ConvertFieldDefinitions(ICecilType DeclaringType, IList<FieldDefinition> FieldDefinitions)
        {
            var cecilFields = FieldDefinitions;
            IField[] fields = new IField[cecilFields.Count];
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i] = new CecilField(DeclaringType, cecilFields[i]);
            }
            return fields;
        }
        public static IProperty[] ConvertPropertyDefinitions(ICecilType DeclaringType, IList<PropertyDefinition> Properties, IList<EventDefinition> Events)
        {
            List<IProperty> properties = new List<IProperty>();
            foreach (var item in Properties)
            {
                properties.Add(new CecilProperty(DeclaringType, item));
            }
            // TODO: add event support
            return properties.ToArray();
        }

        private void InitializeMembers()
        {
            lazyMethods = new Lazy<IMethod[]>(new Func<IMethod[]>(() => ConvertMethodDefinitions(this, GetCecilMethods(), false)));
            lazyProperties = new Lazy<IProperty[]>(new Func<IProperty[]>(() => ConvertPropertyDefinitions(this, GetCecilProperties(), GetCecilEvents())));
            lazyFields = new Lazy<IField[]>(new Func<IField[]>(() => ConvertFieldDefinitions(this, GetCecilFields())));
            lazyCtors = new Lazy<IMethod[]>(new Func<IMethod[]>(() => ConvertMethodDefinitions(this, GetCecilMethods(), true)));
        }

        public ITypeMember[] GetMembers()
        {
            return GetMembers(this);
        }

        private Lazy<IMethod[]> lazyMethods;
        public IMethod[] GetMethods()
        {
            return lazyMethods.Value;
        }
        public IMethod[] Methods { get { return GetMethods(); } }

        private Lazy<IProperty[]> lazyProperties;
        public IProperty[] GetProperties()
        {
            return lazyProperties.Value;
        }
        public IProperty[] Properties { get { return GetProperties(); } }

        private Lazy<IField[]> lazyFields;
        public IField[] GetFields()
        {
            return lazyFields.Value;
        }
        public IField[] Fields { get { return GetFields(); } }

        private Lazy<IMethod[]> lazyCtors;
        public IMethod[] GetConstructors()
        {
            return lazyCtors.Value;
        }
        public IMethod[] Constructors { get { return GetConstructors(); } }

        #endregion

        #region Generics

        public abstract IEnumerable<IType> GetGenericArguments();
        public abstract IEnumerable<IGenericParameter> GetGenericParameters();

        #endregion

        public virtual ICecilGenericMember DeclaringGenericMember
        {
            get
            {
                return DeclaringNamespace as ICecilGenericMember;
            }
        }

        public virtual INamespace DeclaringNamespace
        {
            get
            {
                return GetTypeReference().GetDeclaringNamespace();
            }
        }

        public IArrayType MakeArrayType(int Rank)
        {
            return new CecilArrayType(this, Rank);
        }

        public IPointerType MakePointerType(PointerKind PointerKind)
        {
            return new CecilPointerType(this, PointerKind);
        }

        public IVectorType MakeVectorType(int[] Dimensions)
        {
            return new CecilVectorType(this, Dimensions);
        }

        public IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            return new CecilGenericType(this, TypeArguments);
        }

        protected virtual string GetName()
        {
            var tRef = GetTypeReference();
            if (tRef.HasGenericParameters)
            {
                if (tRef.IsNested)
                {
                    return CecilExtensions.GetFlameGenericName(tRef.Name, GetGenericParameters().Count());
                }
                else
                {
                    return CecilExtensions.GetFlameGenericName(tRef.Name, tRef.GenericParameters.Count);
                }
            }
            else
            {
                return tRef.Name;
            }
        }
        protected virtual string GetFullName()
        {
            return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name);
        }

        private string nameCache;
        public sealed override string Name
        {
            get
            {
                if (nameCache == null)
                {
                    nameCache = GetName();
                }
                return nameCache;
            }
        }

        private string fullNameCache;
        public sealed override string FullName
        {
            get
            {
                if (fullNameCache == null)
                {
                    fullNameCache = GetFullName();
                }
                return fullNameCache;
            }
        }

        #region Static

        public static IGenericParameter[] ConvertGenericParameters(IGenericParameterProvider Owner, Func<IGenericParameterProvider> Resolver, IGenericMember DeclaringMember, AncestryGraph Graph)
        {
            IGenericParameterProvider resolved = null;
            var cecilParameters = Owner.GenericParameters;
            var genericParameters = new IGenericParameter[cecilParameters.Count];
            for (int i = 0; i < genericParameters.Length; i++)
            {
                var param = cecilParameters[i];
                if (param.Name.StartsWith("!"))
                {
                    if (resolved == null)
                    {
                        resolved = Resolver();
                    }
                    param = CecilExtensions.CloneGenericParameter(resolved.GenericParameters[i], Owner);
                }
                genericParameters[i] = new CecilGenericParameter(param, Graph, DeclaringMember);
            }
            return genericParameters;
        }

        public static IType Create(TypeReference Reference)
        {
            return CecilTypeConverter.DefaultConverter.Convert(Reference);
        }
        public static ICecilType CreateCecil(TypeReference Reference)
        {
            return (ICecilType)CecilTypeConverter.CecilPrimitiveConverter.Convert(Reference);
        }

        #region Import

        public static IType Import(Type ImportedType, ICecilMember ImportingMember)
        {
            return Import(ImportedType, ImportingMember.GetModule());
        }
        public static IType Import(Type ImportedType, ModuleDefinition ImportingModule)
        {
            return CecilTypeConverter.DefaultConverter.Convert(ImportingModule.Import(ImportedType));
        }

        public static ICecilType ImportCecil(Type ImportedType, ICecilMember ImportingMember)
        {
            return ImportCecil(ImportedType, ImportingMember.GetModule());
        }
        public static ICecilType ImportCecil(Type ImportedType, ModuleDefinition ImportingModule)
        {
            return (ICecilType)CecilTypeConverter.CecilPrimitiveConverter.Convert(ImportingModule.Import(ImportedType));
        }

        public static ICecilType ImportCecil<T>(ICecilMember ImportingMember)
        {
            return ImportCecil(typeof(T), ImportingMember);
        }
        public static ICecilType ImportCecil<T>(ModuleDefinition ImportingModule)
        {
            return ImportCecil(typeof(T), ImportingModule);
        }

        public static IType Import(TypeReference ImportedType, ICecilMember ImportingMember)
        {
            return Import(ImportedType, ImportingMember.GetModule());
        }
        public static IType Import(TypeReference ImportedType, ModuleDefinition ImportingModule)
        {
            return CecilTypeConverter.DefaultConverter.Convert(ImportingModule.Import(ImportedType));
        }

        public static IType Import<T>(ICecilMember ImportingMember)
        {
            return Import(typeof(T), ImportingMember);
        }
        public static IType Import<T>(ModuleDefinition ImportingModule)
        {
            return Import(typeof(T), ImportingModule);
        }

        #endregion

        #endregion

        #region Comparison

        public virtual bool Equals(ICecilType other)
        {
            return FullName == other.FullName;
        }

        public override bool Equals(object obj)
        {
            if (obj is ICecilType)
            {
                return Equals((ICecilType)obj);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return GetTypeReference().GetHashCode();
        }

        #endregion

        #region Nested Types

        public IType[] GetTypes()
        {
            var nestedTypes = GetTypeReference().Resolve().NestedTypes;
            IType[] results = new IType[nestedTypes.Count];
            for (int i = 0; i < nestedTypes.Count; i++)
            {
                results[i] = Create(nestedTypes[i]);
            }
            return results;
        }

        public IAssembly DeclaringAssembly
        {
            get { return DeclaringNamespace.DeclaringAssembly; }
        }

        #endregion
    }
}
