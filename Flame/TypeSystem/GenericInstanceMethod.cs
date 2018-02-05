using System;
using System.Collections.Generic;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A base type for method specializations.
    /// </summary>
    public abstract class GenericMethodBase : IMethod
    {
        /// <summary>
        /// Creates an uninitialized generic method specialization
        /// from a generic declaration.
        /// </summary>
        /// <param name="declaration">
        /// A generic method declaration.
        /// </param>
        public GenericMethodBase(IMethod declaration)
        {
            this.Declaration = declaration;
        }

        protected static GenericMethodBase InitializeInstance(GenericMethodBase instance)
        {
            instance.InstantiatingVisitor = new TypeMappingVisitor(
                TypeExtensions.GetRecursiveGenericArgumentMapping(instance));

            instance.ReturnType = instance.InstantiatingVisitor.Visit(
                instance.Declaration.ReturnType);
            instance.parameterCache = new Lazy<IReadOnlyList<Parameter>>(
                instance.CreateParameters);
            instance.baseMethodCache = new Lazy<IReadOnlyList<IMethod>>(
                instance.CreateBaseMethods);

            return instance;
        }

        private Lazy<IReadOnlyList<Parameter>> parameterCache;
        private Lazy<IReadOnlyList<IMethod>> baseMethodCache;

        /// <summary>
        /// Gets the visitor that specializes types from this method's
        /// generic declaration to this specialization.
        /// </summary>
        /// <returns>The instantiating visitor.</returns>
        internal TypeMappingVisitor InstantiatingVisitor { get; private set; }

        /// <summary>
        /// Gets the method declaration of which this method is
        /// a specialization.
        /// </summary>
        /// <returns>The method declaration.</returns>
        public IMethod Declaration { get; private set; }

        /// <summary>
        /// Gets the parent type specialization that defines this method
        /// specialization.
        /// </summary>
        /// <returns>The parent type specialization.</returns>
        public abstract IType ParentType { get; }

        /// <inheritdoc/>
        public abstract UnqualifiedName Name { get; }

        /// <inheritdoc/>
        public abstract QualifiedName FullName { get; }

        /// <inheritdoc/>
        public abstract IReadOnlyList<IGenericParameter> GenericParameters { get; }

        /// <inheritdoc/>
        public bool IsConstructor => Declaration.IsConstructor;

        /// <inheritdoc/>
        public bool IsStatic => Declaration.IsStatic;

        /// <inheritdoc/>
        public IType ReturnType { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<Parameter> Parameters => parameterCache.Value;

        private IReadOnlyList<Parameter> CreateParameters()
        {
            var oldParameters = Declaration.Parameters;
            var results = new Parameter[oldParameters.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = oldParameters[i].WithType(
                    InstantiatingVisitor.Visit(oldParameters[i].Type));
            }
            return results;
        }

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> BaseMethods => baseMethodCache.Value;

        /// <inheritdoc/>
        public AttributeMap Attributes => Declaration.Attributes;

        private IReadOnlyList<IMethod> CreateBaseMethods()
        {
            var oldBaseMethods = Declaration.BaseMethods;
            var results = new IMethod[oldBaseMethods.Count];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = InstantiatingVisitor.Visit(oldBaseMethods[i]);
            }
            return results;
        }
    }

    /// <summary>
    /// A specialization of a method that is obtained by specializing
    /// the method's parent type.
    /// </summary>
    public sealed class GenericInstanceMethod : GenericMethodBase
    {
        private GenericInstanceMethod(
            IMethod declaration,
            GenericTypeBase parentType)
            : base(declaration)
        {
            this.parentTy = parentType;
        }

        private static GenericInstanceMethod InitializeInstance(GenericInstanceMethod instance)
        {
            GenericMethodBase.InitializeInstance(instance);
            instance.qualName = instance.Declaration.Name.Qualify(
                instance.parentTy.FullName);

            return instance;
        }

        private GenericTypeBase parentTy;
        private QualifiedName qualName;

        /// <summary>
        /// Gets the parent type specialization that defines this method
        /// specialization.
        /// </summary>
        /// <returns>The parent type specialization.</returns>
        public override IType ParentType => parentTy;

        /// <inheritdoc/>
        public override UnqualifiedName Name => FullName.FullyUnqualifiedName;

        /// <inheritdoc/>
        public override QualifiedName FullName => qualName;

        public override IReadOnlyList<IGenericParameter> GenericParameters
        {
            get
            {
                throw new System.NotImplementedException();
            }
        }

        // This cache interns all generic instance methods: if two
        // GenericInstanceMethod instances (in the wild, not in this
        // private set-up logic) have equal declaration
        // types and type arguments, then they are *referentially* equal.
        private static WeakCache<GenericInstanceMethod, GenericInstanceMethod> instanceCache
            = new WeakCache<GenericInstanceMethod, GenericInstanceMethod>(new StructuralGenericInstanceMethodComparer());

        /// <summary>
        /// Creates a generic instance method from a generic declaration
        /// and a parent type that is itself an (indirect) generic type.
        /// </summary>
        /// <param name="declaration">
        /// The generic declaration to specialize.
        /// </param>
        /// <param name="parentType">
        /// A specialization of the generic declaration's parent type.
        /// </param>
        /// <returns>A specialization of the generic declaration.</returns>
        internal static GenericInstanceMethod Create(
            IMethod declaration,
            GenericTypeBase parentType)
        {
            return instanceCache.Get(
                new GenericInstanceMethod(declaration, parentType),
                InitializeInstance);
        }
    }

    internal sealed class StructuralGenericInstanceMethodComparer : IEqualityComparer<GenericInstanceMethod>
    {
        public bool Equals(GenericInstanceMethod x, GenericInstanceMethod y)
        {
            return object.Equals(x.Declaration, y.Declaration)
                && object.Equals(x.ParentType, y.ParentType);
        }

        public int GetHashCode(GenericInstanceMethod obj)
        {
            return (((object)obj.ParentType).GetHashCode() << 3)
                ^ ((object)obj.Declaration).GetHashCode();
        }
    }

    /// <summary>
    /// A generic method specialization obtained by passing
    /// type arguments directly to a generic declaration.
    /// </summary>
    public sealed class GenericMethod : GenericMethodBase
    {
        public GenericMethod(
            IMethod declaration,
            IReadOnlyList<IType> genericArguments)
            : base(declaration)
        {
            this.GenericArguments = genericArguments;
        }

        private static GenericMethod InitializeInstance(GenericMethod instance)
        {
            var genericArguments = instance.GenericArguments;
            var simpleTypeArgNames = new QualifiedName[genericArguments.Count];
            var qualTypeArgNames = new QualifiedName[simpleTypeArgNames.Length];
            for (int i = 0; i < qualTypeArgNames.Length; i++)
            {
                simpleTypeArgNames[i] = genericArguments[i].Name.Qualify();
                qualTypeArgNames[i] = genericArguments[i].FullName;
            }

            instance.unqualName = new GenericName(instance.Declaration.Name, simpleTypeArgNames);
            instance.qualName = new GenericName(instance.Declaration.FullName, qualTypeArgNames).Qualify();

            GenericMethod.InitializeInstance(instance);

            return instance;
        }

        private UnqualifiedName unqualName;
        private QualifiedName qualName;

        /// <summary>
        /// Gets the generic arguments that were passed to this method.
        /// </summary>
        /// <returns>The generic arguments.</returns>
        public IReadOnlyList<IType> GenericArguments { get; private set; }

        public override IType ParentType => Declaration.ParentType;

        public override UnqualifiedName Name => unqualName;

        public override QualifiedName FullName => qualName;

        public override IReadOnlyList<IGenericParameter> GenericParameters => Declaration.GenericParameters;
    }
}