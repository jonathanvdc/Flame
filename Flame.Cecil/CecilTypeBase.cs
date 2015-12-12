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
        public CecilTypeBase(CecilModule Module)
            : base(Module)
        {
            ClearMemberCaches();
        }

        #region ICecilType Implementation

        public abstract TypeReference GetTypeReference();

        public override MemberReference GetMemberReference()
        {
            return GetTypeReference();
        }

        #endregion

        #region Abstract

        public abstract IEnumerable<IType> BaseTypes { get; }
        protected abstract override IEnumerable<IAttribute> GetMemberAttributes();
        protected abstract override IList<CustomAttribute> GetCustomAttributes();

        public abstract IBoundObject GetDefaultValue();

        protected abstract IList<MethodDefinition> GetCecilMethods();
        protected abstract IList<PropertyDefinition> GetCecilProperties();
        protected abstract IList<FieldDefinition> GetCecilFields();
        protected abstract IList<EventDefinition> GetCecilEvents();

        public abstract IAncestryRules AncestryRules { get; }

        #endregion

        #region Properties

        public virtual bool IsValueType
        {
            get
            {
                return this.GetIsValueType();
            }
        }

        public virtual bool IsInterface
        {
            get
            {
                return this.GetIsInterface();
            }
        }

        #endregion

        #region Type Members

        public static ITypeMember[] GetMembers(ICecilType DeclaringType)
        {
            return DeclaringType.Methods.Concat<ITypeMember>(DeclaringType.Fields).Concat(DeclaringType.Properties).ToArray();
        }
        public static IMethod[] ConvertMethodDefinitions(ICecilType DeclaringType, IList<MethodDefinition> MethodDefinitions)
        {
            List<IMethod> methods = new List<IMethod>();
            foreach (var item in MethodDefinitions)
            {
                if (item.IsConstructor || !item.IsSpecialName)
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

        protected void ClearMemberCaches()
        {
            ClearMethodCache();
            ClearPropertyCache();
            ClearFieldCache();
        }

        protected void ClearMethodCache()
        {
            lazyMethods = new Lazy<IMethod[]>(new Func<IMethod[]>(() => ConvertMethodDefinitions(this, GetCecilMethods())));
        }
        protected void ClearPropertyCache()
        {
            lazyProperties = new Lazy<IProperty[]>(new Func<IProperty[]>(() => ConvertPropertyDefinitions(this, GetCecilProperties(), GetCecilEvents())));
        }
        protected void ClearFieldCache()
        {
            lazyFields = new Lazy<IField[]>(new Func<IField[]>(() => ConvertFieldDefinitions(this, GetCecilFields())));
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
        public IEnumerable<IMethod> Methods { get { return GetMethods(); } }

        private Lazy<IProperty[]> lazyProperties;
        public IProperty[] GetProperties()
        {
            return lazyProperties.Value;
        }
        public IEnumerable<IProperty> Properties { get { return GetProperties(); } }

        private Lazy<IField[]> lazyFields;
        public IField[] GetFields()
        {
            return lazyFields.Value;
        }
        public IEnumerable<IField> Fields { get { return GetFields(); } }

        #endregion

        #region Generics

        public abstract IEnumerable<IGenericParameter> GenericParameters { get; }

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
                return GetTypeReference().GetDeclaringNamespace(Module);
            }
        }

        protected virtual string GetName()
        {
            var tRef = GetTypeReference();
            if (tRef.HasGenericParameters)
            {
                if (tRef.IsNested)
                {
                    return CecilExtensions.GetFlameGenericName(tRef.Name, GenericParameters.Count());
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

        public static IGenericParameter[] ConvertGenericParameters(IGenericParameterProvider Owner, Func<IGenericParameterProvider> Resolver, IGenericMember DeclaringMember, CecilModule Module)
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
                    param = resolved.GenericParameters[i];
                }
                genericParameters[i] = new CecilGenericParameter(param, Module, DeclaringMember);
            }
            return genericParameters;
        }

        #region Import

        public static IType Import(Type ImportedType, ICecilMember ImportingMember)
        {
            return Import(ImportedType, ImportingMember.Module);
        }
        public static IType Import(Type ImportedType, CecilModule ImportingModule)
        {
            return ImportingModule.Convert(ImportingModule.Module.Import(ImportedType));
        }

        public static IType ImportCecil(Type ImportedType, ICecilMember ImportingMember)
        {
            return ImportCecil(ImportedType, ImportingMember.Module);
        }
        public static IType ImportCecil(Type ImportedType, CecilModule ImportingModule)
        {
            return ImportingModule.ConvertStrict(ImportingModule.Module.Import(ImportedType));
        }

        public static IType ImportCecil<T>(ICecilMember ImportingMember)
        {
            return ImportCecil(typeof(T), ImportingMember);
        }
        public static IType ImportCecil<T>(CecilModule ImportingModule)
        {
            return ImportCecil(typeof(T), ImportingModule);
        }

        public static IType Import(TypeReference ImportedType, ICecilMember ImportingMember)
        {
            return Import(ImportedType, ImportingMember.Module);
        }
        public static IType Import(TypeReference ImportedType, CecilModule ImportingModule)
        {
            return ImportingModule.Convert(ImportingModule.Module.Import(ImportedType));
        }

        public static IType Import<T>(ICecilMember ImportingMember)
        {
            return Import(typeof(T), ImportingMember);
        }
        public static IType Import<T>(CecilModule ImportingModule)
        {
            return Import(typeof(T), ImportingModule);
        }

        #endregion

        #endregion

        #region Comparison

        public virtual bool Equals(ICecilType other)
        {
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }

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

        public IEnumerable<IType> Types
        {
            get
            {
                var nestedTypes = GetTypeReference().Resolve().NestedTypes;
                IType[] results = new IType[nestedTypes.Count];
                for (int i = 0; i < nestedTypes.Count; i++)
                {
                    results[i] = Module.Convert(nestedTypes[i]);
                }
                return results;
            }
        }

        public IAssembly DeclaringAssembly
        {
            get { return DeclaringNamespace.DeclaringAssembly; }
        }

        #endregion
    }
}
