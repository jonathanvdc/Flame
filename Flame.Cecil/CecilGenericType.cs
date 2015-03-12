using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilGenericType : ICecilType, IEquatable<ICecilMember>, IEquatable<ICecilType>
    {
        public CecilGenericType(GenericInstanceType GenericInstance)
        {
            this.GenericDeclaration = CecilTypeBase.CreateCecil(GenericInstance.ElementType);
            var genArgs = GenericInstance.GenericArguments;
            IType[] typeArgs = new IType[genArgs.Count];
            for (int i = 0; i < typeArgs.Length; i++)
            {
                typeArgs[i] = CecilTypeBase.Create(genArgs[i]);
            }
            CecilTypeArguments = typeArgs;
        }
        public CecilGenericType(ICecilType GenericDeclaration, IEnumerable<IType> TypeArguments)
        {
            this.GenericDeclaration = GenericDeclaration;
            var genericDecl = GenericDeclaration.GetDeclaringGenericMember();
            if (genericDecl == null)
            {
                this.CecilTypeArguments = TypeArguments.ToArray();
            }
            else if (TypeArguments.Count() == GenericDeclaration.GetCecilGenericParameters().Count())
            {
                this.CecilTypeArguments = TypeArguments.ToArray();
            }
            else
            {
                this.CecilTypeArguments = genericDecl.GetCecilGenericParameters().Prefer(genericDecl.GetCecilGenericArguments()).Concat(TypeArguments).ToArray();
            }
        }

        public static TypeReference CreateGenericInstanceReference(TypeReference GenericDeclaration, IEnumerable<TypeReference> GenericArguments)
        {
            var inst = new GenericInstanceType(GenericDeclaration);
            foreach (var item in GenericArguments)
            {
                inst.GenericArguments.Add(item);
            }
            return inst;
        }

        public ICecilType GenericDeclaration { get; private set; }
        public IType[] CecilTypeArguments { get; private set; }

        private TypeReference typeRef;
        public TypeReference GetTypeReference()
        {
            if (typeRef == null)
            {
                var declTypeRef = GenericDeclaration.GetTypeReference();
                var module = declTypeRef.Module;
                var genericTypeRef = declTypeRef.Resolve();
                var cecilTypeArgs = GetCecilGenericArguments().Select((item) => item.GetTypeReference(module)).ToArray();
                if (cecilTypeArgs.All((item) => item != null))
                {
                    var inst = new GenericInstanceType(genericTypeRef);
                    foreach (var item in cecilTypeArgs)
                    {
                        inst.GenericArguments.Add(item);
                    }
                    typeRef = inst;
                }
                else
                {
                    typeRef = genericTypeRef; // Kind of sad, really. Oh, well.
                }
            }
            return typeRef;
        }

        public MemberReference GetMemberReference()
        {
            return GetTypeReference();
        }

        public IBoundObject GetDefaultValue()
        {
            throw new NotImplementedException();
        }

        public INamespace DeclaringNamespace
        {
            get
            {
                return GetTypeReference().GetDeclaringNamespace();
            }
        }

        #region Container Type

        public bool IsContainerType
        {
            get { return false; }
        }

        public IContainerType AsContainerType()
        {
            return null;
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

        #endregion

        #region GetBaseTypes

        private IType[] cachedBaseTypes;
        public IType[] GetBaseTypes()
        {
            if (cachedBaseTypes == null)
            {
                IType[] genericBaseTypes = GenericDeclaration.GetBaseTypes();
                cachedBaseTypes = new IType[genericBaseTypes.Length];
                for (int i = 0; i < genericBaseTypes.Length; i++)
                {
                    cachedBaseTypes[i] = this.ResolveType(genericBaseTypes[i]);
                }
            }
            return cachedBaseTypes;
        }

        #endregion

        #region IMember Implementation

        private string nameCache;
        public string Name
        {
            get
            {
                if (nameCache == null)
                {
                    StringBuilder sb = new StringBuilder(CecilExtensions.StripCLRGenerics(GenericDeclaration.GetTypeReference().Name));
                    sb.Append('<');
                    for (int i = 0; i < CecilTypeArguments.Length; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }
                        sb.Append(CecilTypeArguments[i].Name);
                    }
                    sb.Append('>');
                    nameCache = sb.ToString();
                }
                return nameCache;
            }
        }

        private string fullNameCache;
        public string FullName
        {
            get
            {
                if (fullNameCache == null)
                {
                    StringBuilder sb = new StringBuilder(CecilExtensions.StripCLRGenerics(GenericDeclaration.GetTypeReference().FullName));
                    sb.Append('<');
                    for (int i = 0; i < CecilTypeArguments.Length; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }
                        sb.Append(CecilTypeArguments[i].FullName);
                    }
                    sb.Append('>');
                    fullNameCache = sb.ToString();
                }
                return fullNameCache;
            }
        }

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
                cachedAttrs = GenericDeclaration.GetAttributes().Select(CompleteAttribute).ToArray();
            }
            return cachedAttrs;
        }

        #endregion

        #region Generics

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return GenericDeclaration.GetGenericParameters();
        }

        public IEnumerable<IType> GetGenericArguments()
        {
            var declGeneric = GenericDeclaration.GetDeclaringGenericMember();
            if (declGeneric != null)
            {
                return CecilTypeArguments.Skip(declGeneric.GetCecilGenericParameters().Count());
            }
            else
            {
                return CecilTypeArguments;
            }
        }

        public IEnumerable<IType> GetCecilGenericArguments()
        {
            return CecilTypeArguments;
        }

        public IEnumerable<IGenericParameter> GetCecilGenericParameters()
        {
            return GenericDeclaration.GetCecilGenericParameters();
        }

        public IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            var genericParams = GenericDeclaration.GetCecilGenericParameters().ToArray();
            for (int i = 0; i < genericParams.Length; i++)
            {
                if (genericParams[i].Equals(TypeParameter))
                {
                    return GetCecilGenericArguments().ElementAt(i);
                }
            }
            string name = TypeParameter.Name;
            if (name.StartsWith("!"))
            {
                int index = int.Parse(name.Substring(1));
                return GetCecilGenericArguments().ElementAt(index);
            }
            return null;
        }

        public IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            return GenericDeclaration.MakeGenericType(TypeArguments);
        }

        public ICecilType GetCecilGenericDeclaration()
        {
            return GenericDeclaration.GetCecilGenericDeclaration();
        }

        public IType GetGenericDeclaration()
        {
            if (GenericDeclaration.get_IsGenericDeclaration())
            {
                return GenericDeclaration;
            }
            else
            {
                return this.GetRelativeGenericDeclaration();
            }
        }

        public bool IsComplete
        {
            get
            {
                return GenericDeclaration.IsComplete && TypeArgumentsComplete;
            }
        }

        private bool TypeArgumentsComplete
        {
            get
            {
                return CecilTypeArguments.All((item) => (item is ICecilType && ((ICecilType)item).IsComplete) || item.get_IsPrimitive());
            }
        }

        #endregion

        #region Type Members

        public ITypeMember[] GetMembers()
        {
            return CecilTypeBase.GetMembers(this);
        }
        public IMethod[] GetMethods()
        {
            return CecilTypeBase.GetMethods(this, GenericDeclaration.GetTypeReference().Resolve().Methods, false);
        }
        public IProperty[] GetProperties()
        {
            var resolvedType = GenericDeclaration.GetTypeReference().Resolve();
            return CecilTypeBase.GetProperties(this, resolvedType.Properties, resolvedType.Events);
        }
        public IField[] GetFields()
        {
            return GenericDeclaration.GetFields().Select(item => new CecilGenericInstanceField(this, (ICecilField)item)).ToArray();
        }
        public IMethod[] GetConstructors()
        {
            return CecilTypeBase.GetMethods(this, GenericDeclaration.GetTypeReference().Resolve().Methods, true);
        }

        #endregion

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
            return GenericDeclaration.GetHashCode() ^ CecilTypeArguments.Aggregate(0, (val, item) => val ^ item.GetHashCode());
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
        public bool Equals(ICecilType other)
        {
            if (GetCecilGenericDeclaration().Equals(other.GetCecilGenericDeclaration()))
            {
                return CecilTypeArguments.AreEqual(other.GetCecilGenericArguments().ToArray());
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Nested Types

        public IType[] GetTypes()
        {
            var nestedTypes = GetTypeReference().Resolve().NestedTypes;
            IType[] results = new IType[nestedTypes.Count];
            for (int i = 0; i < nestedTypes.Count; i++)
            {
                results[i] = CecilTypeBase.ImportCecil(nestedTypes[i], this);
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
