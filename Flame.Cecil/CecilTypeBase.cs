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
        { }
        public CecilTypeBase(AncestryGraph AncestryGraph)
            : base(AncestryGraph)
        { }

        #region ICecilType Implementation

        public abstract TypeReference GetTypeReference();
        public abstract IType ResolveTypeParameter(IGenericParameter TypeParameter);
        public abstract bool IsComplete { get; }

        public override MemberReference GetMemberReference()
        {
            return GetTypeReference();
        }

        #endregion

        #region Abstract

        public abstract IType[] GetBaseTypes();
        public abstract ICecilType GetCecilGenericDeclaration();
        protected abstract override IEnumerable<IAttribute> GetMemberAttributes();
        protected abstract override IList<CustomAttribute> GetCustomAttributes();
        public abstract bool IsContainerType { get; }
        public abstract IContainerType AsContainerType();
        public abstract IEnumerable<IType> GetCecilGenericArguments();


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
        public static IMethod[] GetMethods(ICecilType DeclaringType, IList<MethodDefinition> MethodDefinitions, bool IsConstructor)
        {
            List<IMethod> methods = new List<IMethod>();
            var declRef = DeclaringType.GetTypeReference();
            foreach (var item in MethodDefinitions)
            {
                if (item.IsConstructor == IsConstructor && (item.IsConstructor || !item.IsSpecialName))
                {
                    methods.Add(CecilMethodBase.Create(DeclaringType, item.Reference(DeclaringType)));
                }
            }
            return methods.ToArray();
        }
        public static IField[] GetFields(ICecilType DeclaringType, IList<FieldDefinition> FieldDefinitions)
        {
            var cecilFields = FieldDefinitions;
            IField[] fields = new IField[cecilFields.Count];
            for (int i = 0; i < fields.Length; i++)
            {
                fields[i] = new CecilField(DeclaringType, cecilFields[i].Reference(DeclaringType));
            }
            return fields;
        }
        public static IProperty[] GetProperties(ICecilType DeclaringType, IList<PropertyDefinition> Properties, IList<EventDefinition> Events)
        {
            List<IProperty> properties = new List<IProperty>();
            foreach (var item in Properties)
            {
                properties.Add(new CecilProperty(DeclaringType, item));
            }
            // TODO: add event support
            return properties.ToArray();
        }

        public virtual ITypeMember[] GetMembers()
        {
            return GetMembers(this);
        }
        public virtual IMethod[] GetMethods()
        {
            return GetMethods(this, GetCecilMethods(), false);
        }
        public virtual IProperty[] GetProperties()
        {
            return GetProperties(this, GetCecilProperties(), GetCecilEvents());
        }
        public virtual IField[] GetFields()
        {
            return GetFields(this, GetCecilFields());
        }
        public virtual IMethod[] GetConstructors()
        {
            return GetMethods(this, GetCecilMethods(), true);
        }

        #endregion

        #region Generics

        public virtual IEnumerable<IType> GetGenericArguments()
        {
            var genArgs = GetCecilGenericArguments();
            var declType = DeclaringGenericMember;
            if (declType == null)
            {
                return genArgs;
            }
            else
            {
                return genArgs.Skip(declType.GetCecilGenericParameters().Count());
            }
        }

        public virtual IEnumerable<IGenericParameter> GetCecilGenericParameters()
        {
            var typeRef = GetTypeReference();
            return ConvertGenericParameters(typeRef, typeRef.Resolve, this, AncestryGraph);
        }

        public virtual IEnumerable<IGenericParameter> GetGenericParameters()
        {
            var genParams = GetCecilGenericParameters();
            var declType = DeclaringGenericMember;
            if (declType == null)
            {
                return genParams;
            }
            else
            {
                return genParams.Skip(declType.GetCecilGenericParameters().Count());
            }
        }

        public virtual IType GetGenericDeclaration()
        {
            return this.GetRelativeGenericDeclaration();
        }

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

        public static IGenericParameter[] ConvertGenericParameters(IGenericParameterProvider Owner, Func<IGenericParameterProvider> Resolver,  IGenericMember DeclaringMember, AncestryGraph Graph)
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

        private static IType Create(TypeReference Reference, bool UsePrimitives)
        {
            if (Reference == null)
            {
                return null;
            }
            if (Reference.IsArray)
            {
                var arrayRef = (ArrayType)Reference;
                return Create(arrayRef.ElementType, UsePrimitives).MakeArrayType(arrayRef.Rank);
            }
            else if (Reference.IsPointer)
            {
                return Create(((PointerType)Reference).ElementType, UsePrimitives).MakePointerType(PointerKind.TransientPointer);
            }
            else if (Reference.IsByReference)
            {
                return Create(((ByReferenceType)Reference).ElementType, UsePrimitives).MakePointerType(PointerKind.ReferencePointer);
            }
            else if (Reference.IsGenericInstance)
            {
                var instRef = (GenericInstanceType)Reference;
                var genDecl = Create(instRef.ElementType, UsePrimitives);
                var typeArgs = instRef.GenericArguments.Select((item) => Create(item, UsePrimitives)).ToArray();
                return genDecl.MakeGenericType(typeArgs);
            }
            else if (Reference.IsGenericParameter)
            {
                return new CecilGenericParameter((GenericParameter)Reference);
            }
            else if (UsePrimitives)
            {
                if (Reference is IModifierType)
                {
                    var reqModType = (IModifierType)Reference;
                    if (reqModType.ModifierType.FullName == "System.Runtime.CompilerServices.IsSignUnspecifiedByte")
                    {
                        var elemType = Create(reqModType.ElementType);
                        switch (elemType.GetPrimitiveMagnitude())
                        {
                            case 1:
                                return PrimitiveTypes.Bit8;
                            case 2:
                                return PrimitiveTypes.Bit16;
                            case 3:
                                return PrimitiveTypes.Bit32;
                            case 4:
                                return PrimitiveTypes.Bit64;
                            default:
                                break;
                        }
                    }
                }
                if (Reference.Namespace == "System")
                {
                    switch (Reference.Name)
                    {
                        case "SByte":
                            return PrimitiveTypes.Int8;
                        case "Int16":
                            return PrimitiveTypes.Int16;
                        case "Char":
                            return PrimitiveTypes.Char;
                        case "Int32":
                            return PrimitiveTypes.Int32;
                        case "Int64":
                            return PrimitiveTypes.Int64;
                        case "Byte":
                            return PrimitiveTypes.UInt8;
                        case "UInt16":
                            return PrimitiveTypes.UInt16;
                        case "UInt32":
                            return PrimitiveTypes.UInt32;
                        case "UInt64":
                            return PrimitiveTypes.UInt64;
                        case "Single":
                            return PrimitiveTypes.Float32;
                        case "Double":
                            return PrimitiveTypes.Float64;
                        case "Void":
                            return PrimitiveTypes.Void;
                        case "String":
                            return PrimitiveTypes.String;
                        case "Boolean":
                            return PrimitiveTypes.Boolean;
                        /*case "Object":
                            return CLR.CLRType.Create<object>();*/
                        default:
                            return new CecilType(Reference);
                    }
                }
            }
            return new CecilType(Reference);
        }

        public static IType Create(TypeReference Reference)
        {
            return Create(Reference, true);
        }
        public static ICecilType CreateCecil(TypeReference Reference)
        {
            return (ICecilType)Create(Reference, false);
        }
        /*public static ICecilType Create(TypeDefinition Definition)
        {
            if (Definition == null)
            {
                return null;
            }
            else
            {
                return new CecilType(Definition);
            }
        }*/

        #region Import

        public static ICecilType ImportCecil(TypeReference Type, ICecilMember ImportingMember)
        {
            return ImportCecil(Create(Type), ImportingMember);
        }
        public static ICecilType ImportCecil(TypeReference Type, ModuleDefinition Module)
        {
            return ImportCecil(Create(Type), Module);
        }
        public static ICecilType ImportCecil(IType Type, ICecilMember ImportingMember)
        {
            IType impType = ImportingMember is IGenericResolver ? ((IGenericResolver)ImportingMember).ResolveType(Type) : Type;
            return ImportCecil(impType, ImportingMember.GetModule(), ImportingMember as IGenericMember);
        }
        public static ICecilType ImportCecil(IType Type, ModuleDefinition ImportingModule)
        {
            return ImportCecil(Type, ImportingModule, null);
        }
        private static IType ImportCecilWeak(IType Type, ModuleDefinition ImportingModule, IGenericMember GenericMember)
        {
            var result = ImportCecil(Type, ImportingModule, GenericMember);
            return result == null ? Type : result;
        }
        private static ICecilType ImportCecil(IType Type, ModuleDefinition ImportingModule, IGenericMember GenericMember)
        {
            bool isCecilType = false;
            if (Type is ICecilType)
            {
                isCecilType = true;
                var cecilType = (ICecilType)Type;
                if (cecilType.IsComplete)
                {
                    if (cecilType.GetModule().Name == ImportingModule.Name)
                    {
                        return cecilType;
                    }
                }
                if (cecilType.IsCecilGenericDeclaration())
                {
                    return (ICecilType)CecilTypeBase.Create(ImportingModule.Import(cecilType.GetTypeReference().Resolve()));
                }
                else if (cecilType.IsCecilGenericInstance())
                {
                    var genDecl = ImportCecil(cecilType.GetCecilGenericDeclaration(), ImportingModule, GenericMember);
                    if (genDecl != null)
                    {
                        var typeArgs = cecilType.GetCecilGenericArguments().Select((item) => ImportCecilWeak(item, ImportingModule, GenericMember));
                        return (ICecilType)genDecl.MakeGenericType(typeArgs);
                    }
                }
            }
            if (Type.get_IsGenericInstance())
            {
                var genDecl = ImportCecil(Type.GetGenericDeclaration(), ImportingModule, GenericMember);
                if (genDecl != null)
                {
                    var typeArgs = Type.GetGenericArguments().Select((item) => ImportCecilWeak(item, ImportingModule, GenericMember));
                    return (ICecilType)genDecl.MakeGenericType(typeArgs);
                }
            }
            else if (Type.IsContainerType)
            {
                var container = Type.AsContainerType();
                var elemType = ImportCecil(container.GetElementType(), ImportingModule, GenericMember);
                if (elemType == null)
                {
                    return null;
                }
                if (container.get_IsPointer())
                {
                    return elemType.MakePointerType(container.AsPointerType().PointerKind) as ICecilType;
                }
                else if (container.get_IsVector())
                {
                    return elemType.MakeVectorType(container.AsVectorType().GetDimensions()) as ICecilType;
                }
                else
                {
                    return elemType.MakeArrayType(container.AsArrayType().ArrayRank) as ICecilType;
                }
            }
            else if (GenericMember != null && Type is IGenericParameter)
            {
                var typeParams = GenericMember is ICecilGenericMember ? ((ICecilGenericMember)GenericMember).GetCecilGenericParameters() : GenericMember.GetGenericParameters();
                var match = typeParams.FirstOrDefault((item) => item.Name == Type.Name);
                if (match != null)
                {
                    return (ICecilType)match;
                }
            }
            if (isCecilType)
            {
                var cecilType = (ICecilType)Type;
                var typeRef = cecilType.GetTypeReference();
                try
                {
                    return CreateCecil(ImportingModule.Import(typeRef));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debugger.Break();
                    throw;
                }

            }
            return CreateCecil(Type.GetImportedReference(ImportingModule, null));
        }
        public static IType Import(Type ImportedType, ICecilMember ImportingMember)
        {
            return Import(ImportedType, ImportingMember.GetModule());
        }
        public static IType Import(Type ImportedType, ModuleDefinition ImportingModule)
        {
            return Create(ImportingModule.Import(ImportedType));
        }
        public static IType Import(IType Type, ModuleDefinition Module)
        {
            return Create(ImportCecil(Type, Module).GetTypeReference());
        }
        public static IType Import(IType Type, ICecilMember Member)
        {
            return Create(ImportCecil(Type, Member).GetTypeReference());
        }

        public static IType[] Import(IType[] Types, ModuleDefinition Module)
        {
            IType[] results = new IType[Types.Length];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = Import(Types[i], Module);
            }
            return results;
        }
        public static IType[] Import(IType[] Types, ICecilMember Member)
        {
            return Import(Types, Member.GetModule());
        }

        public static IType Import(TypeReference ImportedType, ICecilMember ImportingMember)
        {
            return Create(ImportCecil(ImportedType, ImportingMember.GetModule()).GetTypeReference(), true);
        }

        public static ICecilType ImportCecil(Type ImportedType, ICecilMember ImportingMember)
        {
            return ImportCecil(ImportedType, ImportingMember.GetModule());
        }
        public static ICecilType ImportCecil(Type ImportedType, ModuleDefinition ImportingModule)
        {
            return CreateCecil(ImportingModule.Import(ImportedType));
        }
        public static ICecilType ImportCecil<T>(ICecilMember ImportingMember)
        {
            return ImportCecil(typeof(T), ImportingMember);
        }
        public static ICecilType ImportCecil<T>(ModuleDefinition ImportingModule)
        {
            return ImportCecil(typeof(T), ImportingModule);
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
                results[i] = ImportCecil(nestedTypes[i], this);
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
