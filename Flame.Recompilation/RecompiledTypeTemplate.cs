using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledTypeTemplate : RecompiledMemberTemplate, IType
    {
        protected RecompiledTypeTemplate(AssemblyRecompiler Recompiler, IType SourceType)
            : base(Recompiler)
        {
            this.SourceType = SourceType;
        }

        #region Static

        public static IType GetRecompilerTemplate(AssemblyRecompiler Recompiler, IType SourceType)
        {
            if (Recompiler.IsExternal(SourceType))
            {
                return SourceType;
            }
            else if (SourceType.get_IsGenericInstance())
            {
                return new RecompiledTypeTemplate(Recompiler, SourceType.GetGenericDeclaration()).MakeGenericType(GetRecompilerTemplates(Recompiler, SourceType.GetGenericArguments()));
            }
            else if (SourceType.IsContainerType)
            {
                var container = SourceType.AsContainerType();
                var elem = new RecompiledTypeTemplate(Recompiler, container.ElementType);
                if (elem.get_IsVector())
                {
                    return elem.MakeVectorType(container.AsVectorType().GetDimensions());
                }
                else if (elem.get_IsArray())
                {
                    return elem.MakeArrayType(container.AsArrayType().ArrayRank);
                }
                else if (elem.get_IsPointer())
                {
                    return elem.MakePointerType(container.AsPointerType().PointerKind);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                return new RecompiledTypeTemplate(Recompiler, SourceType);
            }
        }
        public static IReadOnlyList<IType> GetRecompilerTemplates(AssemblyRecompiler Recompiler, IEnumerable<IType> SourceTypes)
        {
            List<IType> templates = new List<IType>();
            foreach (var item in SourceTypes)
            {
                templates.Add(GetRecompilerTemplate(Recompiler, item));
            }
            return templates;
        }
        public static IType[] GetRecompilerTemplates(AssemblyRecompiler Recompiler, IType[] SourceTypes)
        {
            IType[] templates = new IType[SourceTypes.Length];
            for (int i = 0; i < SourceTypes.Length; i++)
            {
                templates[i] = GetRecompilerTemplate(Recompiler, SourceTypes[i]);
            }
            return templates;
        }

        #endregion

        public IType SourceType { get; private set; }
        public override IMember GetSourceMember()
        {
            return SourceType;
        }

        #region Members

        public ITypeMember[] GetMembers()
        {
            return Recompiler.GetTypeMembers(SourceType.GetMembers());
        }

        public IMethod[] GetConstructors()
        {
            return Recompiler.MethodCache.GetMany(SourceType.GetConstructors());
        }

        public IField[] GetFields()
        {
            return Recompiler.FieldCache.GetMany(SourceType.GetFields());
        }

        public IMethod[] GetMethods()
        {
            return Recompiler.MethodCache.GetMany(SourceType.GetMethods());
        }

        public IProperty[] GetProperties()
        {
            return Recompiler.PropertyCache.GetMany(SourceType.GetProperties());
        }

        #endregion

        public INamespace DeclaringNamespace
        {
            get { return Recompiler.NamespaceCache.Get(SourceType.DeclaringNamespace); }
        }

        public IType[] GetBaseTypes()
        {
            return GetWeakRecompiledTypes(SourceType.GetBaseTypes(), Recompiler, SourceType);
        }

        public IBoundObject GetDefaultValue()
        {
            return SourceType.GetDefaultValue();
        }

        #region Generics

        #region Static

        public static IType[] GetWeakRecompiledTypes(IType[] SourceTypes, AssemblyRecompiler Recompiler, IGenericMember DeclaringMember)
        {
            IType[] results = new IType[SourceTypes.Length];
            for (int i = 0; i < SourceTypes.Length; i++)
            {
                results[i] = GetWeakRecompiledType(SourceTypes[i], Recompiler, DeclaringMember);
            }
            return results;
        }

        public static IType GetWeakRecompiledType(IType SourceType, AssemblyRecompiler Recompiler, IGenericMember DeclaringMember)
        {
            if (SourceType.get_IsGenericParameter())
            {
                if (((IGenericParameter)SourceType).DeclaringMember.Equals(DeclaringMember))
                {
                    return SourceType;
                }
            }
            if (SourceType.get_IsDelegate())
            {
                return MethodType.Create(
                        RecompiledMethodTemplate.GetWeakRecompiledMethod(
                                         MethodType.GetMethod(SourceType), 
                                         Recompiler, 
                                         DeclaringMember));
            }
            else if (SourceType.get_IsIntersectionType())
            {
                var interType = (IntersectionType)SourceType;

                return new IntersectionType(GetWeakRecompiledType(interType.First, Recompiler, DeclaringMember),
                                            GetWeakRecompiledType(interType.Second, Recompiler, DeclaringMember));
            }
            else if (SourceType.IsContainerType)
            {
                var containerType = SourceType.AsContainerType();
                var recompiledElemType = GetWeakRecompiledType(containerType.ElementType, Recompiler, DeclaringMember);
                if (SourceType.get_IsVector())
                {
                    return recompiledElemType.MakeVectorType(containerType.AsVectorType().GetDimensions());
                }
                else if (SourceType.get_IsPointer())
                {
                    return recompiledElemType.MakePointerType(containerType.AsPointerType().PointerKind);
                }
                else if (SourceType.get_IsArray())
                {
                    return recompiledElemType.MakeArrayType(containerType.AsArrayType().ArrayRank);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else if (SourceType.get_IsGenericInstance())
            {
                var genericDecl = SourceType.GetGenericDeclaration();
                var recompiledGenericDecl = GetWeakRecompiledType(genericDecl, Recompiler, DeclaringMember);
                var genericArgs = SourceType.GetGenericArguments();
                var recompiledGenericArgs = GetWeakRecompiledTypes(genericArgs.ToArray(), Recompiler, DeclaringMember);
                return recompiledGenericDecl.MakeGenericType(recompiledGenericArgs);
            }
            else
            {
                return Recompiler.GetType(SourceType);
            }
        }

        #endregion

        public IType GetGenericDeclaration()
        {
            return this;
        }

        public IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            return Recompiler.GetType(SourceType).MakeGenericType(TypeArguments);
        }

        public IEnumerable<IType> GetGenericArguments()
        {
            return SourceType.GetGenericArguments();
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return RecompiledGenericParameterTemplate.GetRecompilerTemplates(Recompiler, this, SourceType.GenericParameters);
        }

        #endregion

        #region Container Types

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
            return new DescribedArrayType(this, Rank);
        }

        public IPointerType MakePointerType(PointerKind PointerKind)
        {
            return new DescribedPointerType(this, PointerKind);
        }

        public IVectorType MakeVectorType(int[] Dimensions)
        {
            return new DescribedVectorType(this, Dimensions);
        }

        #endregion
    }
}
