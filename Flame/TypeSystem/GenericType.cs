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
        /// Creates a generic type from a declaration.
        /// </summary>
        /// <param name="declaration">A declaration.</param>
        internal GenericTypeBase(IType declaration)
        {
            this.Declaration = declaration;
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
        public abstract override bool Equals(object obj);

        /// <inheritdoc/>
        public abstract override int GetHashCode();

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
        public IReadOnlyList<IField> Fields => throw new System.NotImplementedException();

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> Methods => throw new System.NotImplementedException();

        /// <inheritdoc/>
        public IReadOnlyList<IProperty> Properties => throw new System.NotImplementedException();

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
    public sealed class GenericType : GenericTypeBase, IEquatable<GenericType>
    {
        internal GenericType(
            IType declaration,
            IReadOnlyList<IType> genericArguments)
            : base(declaration)
        {
            this.GenericArguments = genericArguments;

            var simpleTypeArgNames = new QualifiedName[genericArguments.Count];
            var qualTypeArgNames = new QualifiedName[simpleTypeArgNames.Length];
            for (int i = 0; i < qualTypeArgNames.Length; i++)
            {
                simpleTypeArgNames[i] = genericArguments[i].Name.Qualify();
                qualTypeArgNames[i] = genericArguments[i].FullName;
            }

            this.simpleName = new GenericName(declaration.Name, simpleTypeArgNames);
            this.qualName = new GenericName(declaration.FullName, qualTypeArgNames).Qualify();
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

        /// <summary>
        /// Checks if this generic type equals another.
        /// </summary>
        /// <param name="other">A generic type.</param>
        /// <returns>
        /// <c>true</c> if the types are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(GenericType other)
        {
            return object.ReferenceEquals(this, other)
                || (object.Equals(Declaration, other.Declaration)
                    && Enumerable.SequenceEqual<IType>(
                        GenericArguments, other.GenericArguments));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is GenericType && Equals((GenericType)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int result = ((object)Declaration).GetHashCode();
            int genericArgCount = GenericArguments.Count;
            for (int i = 0; i < genericArgCount; i++)
            {
                result = (result << 2) ^ ((object)GenericArguments[i]).GetHashCode();
            }
            return result;
        }
    }

    /// <summary>
    /// A type that is defined in an instantiated generic type.
    /// </summary>
    public sealed class GenericInstanceType : GenericTypeBase, IEquatable<GenericInstanceType>
    {
        private GenericInstanceType(
            IType declaration,
            GenericTypeBase parentType)
            : base(declaration)
        {
            this.ParentType = parentType;
            this.qualName = Declaration.Name.Qualify(ParentType.FullName);
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

        /// <summary>
        /// Checks if this generic instance type equals another.
        /// </summary>
        /// <param name="other">A generic instance type.</param>
        /// <returns>
        /// <c>true</c> if the types are equal; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(GenericInstanceType other)
        {
            return object.ReferenceEquals(this, other)
                || (object.Equals(Declaration, other.Declaration)
                    && object.Equals(ParentType, other.ParentType));
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj is GenericInstanceType && Equals((GenericInstanceType)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (((object)ParentType).GetHashCode() << 4) ^ ((object)Declaration).GetHashCode();
        }

        private static ThreadLocal<LruCache<Tuple<IType, GenericTypeBase>, GenericInstanceType>> instanceCache
            = new ThreadLocal<LruCache<Tuple<IType, GenericTypeBase>, GenericInstanceType>>(
                createInstanceCache);

        private static LruCache<Tuple<IType, GenericTypeBase>, GenericInstanceType> createInstanceCache()
        {
            return new LruCache<Tuple<IType, GenericTypeBase>, GenericInstanceType>(
                TypeExtensions.TypeCacheCapacity);
        }

        internal static GenericInstanceType Create(
            IType declaration,
            GenericTypeBase parentType)
        {
            return instanceCache.Value.Get(
                new Tuple<IType, GenericTypeBase>(declaration, parentType),
                CreateImpl);
        }

        private static GenericInstanceType CreateImpl(
            Tuple<IType, GenericTypeBase> arg)
        {
            return new GenericInstanceType(arg.Item1, arg.Item2);
        }
    }
}