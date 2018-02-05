using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Collections;

namespace Flame.TypeSystem
{
    /// <summary>
    /// A base type for method specializations.
    /// </summary>
    public abstract class MethodSpecialization : IMethod
    {
        /// <summary>
        /// Creates an uninitialized generic method specialization
        /// from a generic declaration.
        /// </summary>
        /// <param name="declaration">
        /// A generic method declaration.
        /// </param>
        public MethodSpecialization(IMethod declaration)
        {
            this.Declaration = declaration;
        }

        protected static MethodSpecialization InitializeInstance(MethodSpecialization instance)
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
    public sealed class IndirectMethodSpecialization : MethodSpecialization
    {
        private IndirectMethodSpecialization(
            IMethod declaration,
            TypeSpecialization parentType)
            : base(declaration)
        {
            this.parentTy = parentType;
        }

        private static IndirectMethodSpecialization InitializeInstance(IndirectMethodSpecialization instance)
        {
            MethodSpecialization.InitializeInstance(instance);
            instance.qualName = instance.Declaration.Name.Qualify(
                instance.parentTy.FullName);
            instance.genericParameterCache = new Lazy<IReadOnlyList<IGenericParameter>>(
                instance.CreateGenericParameters);

            return instance;
        }

        private TypeSpecialization parentTy;
        private QualifiedName qualName;
        private Lazy<IReadOnlyList<IGenericParameter>> genericParameterCache;

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

        /// <inheritdoc/>
        public override IReadOnlyList<IGenericParameter> GenericParameters =>
            genericParameterCache.Value;

        private IReadOnlyList<IGenericParameter> CreateGenericParameters()
        {
            return IndirectGenericParameterSpecialization.CreateAll(Declaration, this);
        }

        // This cache interns all indirect method specializations: if two
        // IndirectMethodSpecialization instances (in the wild, not in this
        // private set-up logic) have equal declaration
        // types and parent types, then they are *referentially* equal.
        private static WeakCache<IndirectMethodSpecialization, IndirectMethodSpecialization> instanceCache
            = new WeakCache<IndirectMethodSpecialization, IndirectMethodSpecialization>(
                new StructuralIndirectMethodSpecializationComparer());

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
        internal static IndirectMethodSpecialization Create(
            IMethod declaration,
            TypeSpecialization parentType)
        {
            return instanceCache.Get(
                new IndirectMethodSpecialization(declaration, parentType),
                InitializeInstance);
        }
    }

    internal sealed class StructuralIndirectMethodSpecializationComparer : IEqualityComparer<IndirectMethodSpecialization>
    {
        public bool Equals(IndirectMethodSpecialization x, IndirectMethodSpecialization y)
        {
            return object.Equals(x.Declaration, y.Declaration)
                && object.Equals(x.ParentType, y.ParentType);
        }

        public int GetHashCode(IndirectMethodSpecialization obj)
        {
            return (((object)obj.ParentType).GetHashCode() << 3)
                ^ ((object)obj.Declaration).GetHashCode();
        }
    }

    /// <summary>
    /// A generic method specialization obtained by passing
    /// type arguments directly to a generic declaration.
    /// </summary>
    public sealed class DirectMethodSpecialization : MethodSpecialization
    {
        public DirectMethodSpecialization(
            IMethod declaration,
            IReadOnlyList<IType> genericArguments)
            : base(declaration)
        {
            this.GenericArguments = genericArguments;
        }

        private static DirectMethodSpecialization InitializeInstance(DirectMethodSpecialization instance)
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

            MethodSpecialization.InitializeInstance(instance);

            return instance;
        }

        private UnqualifiedName unqualName;
        private QualifiedName qualName;

        /// <summary>
        /// Gets the generic arguments that were passed to this method.
        /// </summary>
        /// <returns>The generic arguments.</returns>
        public IReadOnlyList<IType> GenericArguments { get; private set; }

        /// <inheritdoc/>
        public override IType ParentType => Declaration.ParentType;

        /// <inheritdoc/>
        public override UnqualifiedName Name => unqualName;

        /// <inheritdoc/>
        public override QualifiedName FullName => qualName;

        /// <inheritdoc/>
        public override IReadOnlyList<IGenericParameter> GenericParameters =>
            EmptyArray<IGenericParameter>.Value;

        // This cache interns all direct method specializations: if two
        // DirectMethodSpecialization instances (in the wild, not in this
        // private set-up logic) have equal declaration
        // types and type arguments, then they are *referentially* equal.
        private static WeakCache<DirectMethodSpecialization, DirectMethodSpecialization> instanceCache
            = new WeakCache<DirectMethodSpecialization, DirectMethodSpecialization>(
                new StructuralDirectMethodSpecializationComparer());

        /// <summary>
        /// Creates a direct generic specialization of a particular
        /// generic method declaration.
        /// </summary>
        /// <param name="declaration">
        /// The generic method declaration that is specialized into
        /// a concrete method.
        /// </param>
        /// <param name="genericArguments">
        /// The type arguments with which the generic method is
        /// specialized.
        /// </param>
        /// <returns>A generic specialization.</returns>
        internal static DirectMethodSpecialization Create(
            IMethod declaration,
            IReadOnlyList<IType> genericArguments)
        {
            return instanceCache.Get(
                new DirectMethodSpecialization(declaration, genericArguments),
                InitializeInstance);
        }
    }

    internal sealed class StructuralDirectMethodSpecializationComparer : IEqualityComparer<DirectMethodSpecialization>
    {
        public bool Equals(DirectMethodSpecialization x, DirectMethodSpecialization y)
        {
            return object.Equals(x, y.Declaration)
                && Enumerable.SequenceEqual<IType>(
                    x.GenericArguments, y.GenericArguments);
        }

        public int GetHashCode(DirectMethodSpecialization obj)
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
}