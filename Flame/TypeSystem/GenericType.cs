using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A base type for generic instance types.
    /// </summary>
    public abstract class GenericTypeBase : IType
    {
        /// <summary>
        /// Creates an uninitialized generic type from a declaration.
        /// </summary>
        /// <param name="declaration">A declaration.</param>
        internal GenericTypeBase(IType declaration)
        {
            this.Declaration = declaration;
        }

        /// <summary>
        /// Initializes a generic type.
        /// </summary>
        protected void Initialize()
        {
            this.instantiatingVisitorCache = new Lazy<TypeMappingVisitor>(CreateInstantiatingVisitor);
            this.nestedTypesCache = new Lazy<IReadOnlyList<IType>>(CreateNestedTypes);
            this.baseTypeCache = new Lazy<IReadOnlyList<IType>>(CreateBaseTypes);
        }

        /// <summary>
        /// Gets the generic type declaration this type instantiates.
        /// </summary>
        /// <returns>The generic type declaration.</returns>
        public IType Declaration { get; private set; }

        /// <inheritdoc/>
        public abstract TypeParent Parent { get; }

        /// <inheritdoc/>
        public abstract UnqualifiedName Name { get; }

        /// <inheritdoc/>
        public abstract QualifiedName FullName { get; }

        /// <inheritdoc/>
        public IReadOnlyList<IType> BaseTypes => baseTypeCache.Value;

        private Lazy<IReadOnlyList<IType>> baseTypeCache;

        private IReadOnlyList<IType> CreateBaseTypes()
        {
            return instantiatingVisitorCache.Value.VisitAll(Declaration.BaseTypes);
        }

        /// <inheritdoc/>
        public IReadOnlyList<IField> Fields
        {
            get
            {
                throw new System.NotImplementedException();
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> Methods
        {
            get
            {
                throw new System.NotImplementedException();
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IProperty> Properties
        {
            get
            {
                throw new System.NotImplementedException();
            }
        }

        /// <inheritdoc/>
        public AttributeMap Attributes => Declaration.Attributes;

        /// <inheritdoc/>
        public IReadOnlyList<IGenericParameter> GenericParameters =>
            Declaration.GenericParameters;

        /// <inheritdoc/>
        public IReadOnlyList<IType> NestedTypes => nestedTypesCache.Value;

        private Lazy<IReadOnlyList<IType>> nestedTypesCache;

        private IReadOnlyList<IType> CreateNestedTypes()
        {
            var nestedTypeDecls = Declaration.NestedTypes;
            var results = new IType[nestedTypeDecls.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = GenericInstanceType.Create(nestedTypeDecls[i], this);
            }
            return results;
        }

        private Lazy<TypeMappingVisitor> instantiatingVisitorCache;

        private TypeMappingVisitor CreateInstantiatingVisitor()
        {
            var allParams = this.GetRecursiveGenericParameters();
            var allArgs = Declaration.GetRecursiveGenericArguments();

            int argCount = allArgs.Count;
            var mapping = new Dictionary<IType, IType>();
            for (int i = 0; i < argCount; i++)
            {
                mapping[allParams[i]] = allArgs[i];
            }

            return new TypeMappingVisitor(mapping);
        }
    }

    /// <summary>
    /// A generic type that is instantiated with a list of type arguments.
    /// </summary>
    public sealed class GenericType : GenericTypeBase
    {
        private GenericType(
            IType declaration,
            IReadOnlyList<IType> genericArguments)
            : base(declaration)
        {
            this.GenericArguments = genericArguments;
        }

        private static GenericType InitializeInstance(GenericType instance)
        {
            var declaration = instance.Declaration;
            var genericArguments = instance.GenericArguments;
            var simpleTypeArgNames = new QualifiedName[genericArguments.Count];
            var qualTypeArgNames = new QualifiedName[simpleTypeArgNames.Length];
            for (int i = 0; i < qualTypeArgNames.Length; i++)
            {
                simpleTypeArgNames[i] = genericArguments[i].Name.Qualify();
                qualTypeArgNames[i] = genericArguments[i].FullName;
            }

            instance.simpleName = new GenericName(declaration.Name, simpleTypeArgNames);
            instance.qualName = new GenericName(declaration.FullName, qualTypeArgNames).Qualify();

            instance.Initialize();

            return instance;
        }

        /// <summary>
        /// Gets this generic type's list of generic arguments.
        /// </summary>
        /// <returns>The generic arguments.</returns>
        public IReadOnlyList<IType> GenericArguments { get; private set; }

        private UnqualifiedName simpleName;
        private QualifiedName qualName;

        /// <inheritdoc/>
        public override TypeParent Parent => Declaration.Parent;

        /// <inheritdoc/>
        public override UnqualifiedName Name => simpleName;

        /// <inheritdoc/>
        public override QualifiedName FullName => qualName;

        // This cache interns all generic types: if two GenericType instances
        // (in the wild, not in this private set-up logic) have equal declaration
        // types and type arguments, then they are *referentially* equal.
        private static WeakCache<GenericType, GenericType> GenericTypeCache
            = new WeakCache<GenericType, GenericType>(new StructuralGenericTypeComparer());

        /// <summary>
        /// Creates a generic specialization of a particular generic
        /// type declaration
        /// </summary>
        /// <param name="declaration">
        /// The generic type declaration that is specialized into
        /// a concrete type.
        /// </param>
        /// <param name="genericArguments">
        /// The type arguments with which the generic type is
        /// specialized.
        /// </param>
        /// <returns>A generic specialization.</returns>
        internal static GenericType Create(
            IType declaration,
            IReadOnlyList<IType> genericArguments)
        {
            return GenericTypeCache.Get(
                new GenericType(declaration, genericArguments),
                InitializeInstance);
        }
    }

    internal sealed class StructuralGenericTypeComparer : IEqualityComparer<GenericType>
    {
        public bool Equals(GenericType x, GenericType y)
        {
            return object.Equals(x, y.Declaration)
                && Enumerable.SequenceEqual<IType>(
                    x.GenericArguments, y.GenericArguments);
        }

        public int GetHashCode(GenericType obj)
        {
            int result = ((object)obj.Declaration).GetHashCode();
            int genericArgCount = obj.GenericArguments.Count;
            for (int i = 0; i < genericArgCount; i++)
            {
                result = (result << 2) ^ ((object)obj.GenericArguments[i]).GetHashCode();
            }
            return result;
        }
    }

    /// <summary>
    /// A type that is defined in an instantiated generic type.
    /// </summary>
    public sealed class GenericInstanceType : GenericTypeBase
    {
        /// <summary>
        /// Creates an uninitialized generic instance type.
        /// </summary>
        /// <param name="declaration">The type's declaration.</param>
        /// <param name="parentType">The type's parent type.</param>
        private GenericInstanceType(
            IType declaration,
            GenericTypeBase parentType)
            : base(declaration)
        {
            this.ParentType = parentType;
        }

        private static GenericInstanceType InitializeInstance(GenericInstanceType instance)
        {
            instance.qualName = instance.Declaration.Name.Qualify(
                instance.ParentType.FullName);

            instance.Initialize();

            return instance;
        }

        /// <summary>
        /// Gets the parent type of this generic instance type.
        /// </summary>
        /// <returns>The parent type.</returns>
        public GenericTypeBase ParentType { get; private set; }

        /// <inheritdoc/>
        public override TypeParent Parent => new TypeParent(ParentType);

        private QualifiedName qualName;

        /// <inheritdoc/>
        public override UnqualifiedName Name => qualName.FullyUnqualifiedName;

        /// <inheritdoc/>
        public override QualifiedName FullName => qualName;

        private static WeakCache<GenericInstanceType, GenericInstanceType> instanceCache =
            new WeakCache<GenericInstanceType, GenericInstanceType>(
                new StructuralGenericInstanceTypeComparer());

        /// <summary>
        /// Creates a generic instance type from a generic declaration
        /// and a parent type that is itself an (indirect) generic type.
        /// </summary>
        /// <param name="declaration">
        /// The generic declaration to specialize.
        /// </param>
        /// <param name="parentType">
        /// A specialization of the generic declaration's parent type.
        /// </param>
        /// <returns></returns>
        public static GenericInstanceType Create(
            IType declaration,
            GenericTypeBase parentType)
        {
            return instanceCache.Get(
                new GenericInstanceType(declaration, parentType),
                InitializeInstance);
        }
    }

    internal sealed class StructuralGenericInstanceTypeComparer : IEqualityComparer<GenericInstanceType>
    {
        public bool Equals(GenericInstanceType x, GenericInstanceType y)
        {
            return object.Equals(x, y.Declaration)
                && object.Equals(x.ParentType, y.ParentType);
        }

        public int GetHashCode(GenericInstanceType obj)
        {
            return (((object)obj.ParentType).GetHashCode() << 4) ^ ((object)obj.Declaration).GetHashCode();
        }
    }
}