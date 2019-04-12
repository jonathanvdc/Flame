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

        /// <summary>
        /// Initializes a method specialization instance.
        /// </summary>
        /// <param name="instance">The instance to initialize.</param>
        /// <returns><paramref name="instance"/> itself.</returns>
        protected static MethodSpecialization InitializeInstance(MethodSpecialization instance)
        {
            instance.InstantiatingVisitor = new TypeMappingVisitor(
                TypeExtensions.GetRecursiveGenericArgumentMapping(instance));

            instance.ReturnParameter = instance.InstantiatingVisitor.Visit(
                instance.Declaration.ReturnParameter);
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
        public Parameter ReturnParameter { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyList<Parameter> Parameters => parameterCache.Value;

        private IReadOnlyList<Parameter> CreateParameters()
        {
            return InstantiatingVisitor.VisitAll(Declaration.Parameters);
        }

        /// <inheritdoc/>
        public IReadOnlyList<IMethod> BaseMethods => baseMethodCache.Value;

        /// <inheritdoc/>
        public AttributeMap Attributes => Declaration.Attributes;

        private IReadOnlyList<IMethod> CreateBaseMethods()
        {
            return InstantiatingVisitor.VisitAll(Declaration.BaseMethods);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return FullName.ToString();
        }
    }

    /// <summary>
    /// A specialization of a method that is obtained by specializing
    /// the method's parent type.
    /// </summary>
    public class IndirectMethodSpecialization : MethodSpecialization
    {
        internal IndirectMethodSpecialization(
            IMethod declaration,
            TypeSpecialization parentType)
            : base(declaration)
        {
            this.parentTy = parentType;
        }

        private static IndirectMethodSpecialization InitializeInstance(IndirectMethodSpecialization instance)
        {
            instance.genericParameterCache = new Lazy<IReadOnlyList<IGenericParameter>>(
                instance.CreateGenericParameters);

            MethodSpecialization.InitializeInstance(instance);
            instance.qualName = instance.Declaration.Name.Qualify(
                instance.parentTy.FullName);

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
        private static InterningCache<IndirectMethodSpecialization> instanceCache
            = new InterningCache<IndirectMethodSpecialization>(
                new StructuralIndirectMethodSpecializationComparer(),
                InitializeInstance);

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
            if (declaration is IAccessor)
            {
                var accessor = (IAccessor)declaration;
                return Create(
                    accessor,
                    IndirectPropertySpecialization.Create(
                        accessor.ParentProperty,
                        parentType));
            }
            else
            {
                return instanceCache.Intern(
                    new IndirectMethodSpecialization(declaration, parentType));
            }
        }

        /// <summary>
        /// Creates a generic instance accessor from a generic declaration
        /// and a parent property that is itself an indirect property specialization.
        /// </summary>
        /// <param name="declaration">
        /// The generic declaration to specialize.
        /// </param>
        /// <param name="parentProperty">
        /// A specialization of the generic declaration's parent type.
        /// </param>
        /// <returns>A specialization of the generic declaration.</returns>
        internal static IndirectAccessorSpecialization Create(
            IAccessor declaration,
            IndirectPropertySpecialization parentProperty)
        {
            var accessor = (IAccessor)declaration;
            return (IndirectAccessorSpecialization)instanceCache.Intern(
                new IndirectAccessorSpecialization(
                    accessor,
                    parentProperty));
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
        /// <summary>
        /// Creates a direct method specialization.
        /// </summary>
        /// <param name="declaration">
        /// The generic method to specialize.
        /// </param>
        /// <param name="genericArguments">
        /// A sequence of type arguments to specialize the method with.
        /// </param>
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
        private static InterningCache<DirectMethodSpecialization> instanceCache
            = new InterningCache<DirectMethodSpecialization>(
                new StructuralDirectMethodSpecializationComparer(),
                InitializeInstance);

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
            return instanceCache.Intern(
                new DirectMethodSpecialization(declaration, genericArguments));
        }
    }

    internal sealed class StructuralDirectMethodSpecializationComparer : IEqualityComparer<DirectMethodSpecialization>
    {
        public bool Equals(DirectMethodSpecialization x, DirectMethodSpecialization y)
        {
            return object.Equals(x.Declaration, y.Declaration)
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