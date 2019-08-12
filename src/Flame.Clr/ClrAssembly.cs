using System;
using System.Collections.Generic;
using Flame.Collections;
using Flame.TypeSystem;
using Mono.Cecil;

namespace Flame.Clr
{
    /// <summary>
    /// A Flame assembly that wraps a Cecil assembly definition.
    /// </summary>
    public sealed class ClrAssembly : IAssembly
    {
        /// <summary>
        /// Creates a Flame assembly that wraps a Cecil assembly
        /// definition.
        /// </summary>
        /// <param name="definition">
        /// The assembly definition to wrap.
        /// </param>
        /// <param name="resolver">
        /// The assembly resolver to use.
        /// </param>
        /// <param name="typeEnvironment">
        /// The type environment to use.
        /// </param>
        public ClrAssembly(
            AssemblyDefinition definition,
            AssemblyResolver resolver,
            TypeEnvironment typeEnvironment)
            : this(definition, new ReferenceResolver(resolver, typeEnvironment))
        { }

        /// <summary>
        /// Creates a Flame assembly that wraps a Cecil assembly
        /// definition.
        /// </summary>
        /// <param name="definition">
        /// The assembly definition to wrap.
        /// </param>
        /// <param name="resolver">
        /// The reference resolver to use.
        /// </param>
        public ClrAssembly(
            AssemblyDefinition definition,
            ReferenceResolver resolver)
        {
            this.Definition = definition;
            this.Resolver = resolver;
            this.FullName = new SimpleName(definition.Name.Name).Qualify();
            this.SyncRoot = new object();
            this.typeCache = CreateSynchronizedLazy(AnalyzeTypes);
            this.attributeCache = CreateSynchronizedLazy(AnalyzeAttributes);
        }

        /// <summary>
        /// Gets the Cecil assembly definition wrapped by this assembly.
        /// </summary>
        /// <returns>A Cecil assembly definition.</returns>
        public AssemblyDefinition Definition { get; private set; }

        /// <summary>
        /// Gets the reference resolver used by this assembly.
        /// </summary>
        /// <returns>The reference resolver.</returns>
        public ReferenceResolver Resolver { get; private set; }

        /// <summary>
        /// Gets the object that is used for synchronizing access to
        /// the IL assembly wrapped by this Flame assembly.
        /// </summary>
        /// <returns>A synchronization object.</returns>
        public object SyncRoot { get; private set; }

        /// <inheritdoc/>
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public AttributeMap Attributes => attributeCache.Value;

        private Lazy<AttributeMap> attributeCache;

        /// <inheritdoc/>
        public IReadOnlyList<IType> Types => typeCache.Value;

        private Lazy<IReadOnlyList<IType>> typeCache;

        private IReadOnlyList<IType> AnalyzeTypes()
        {
            var results = new List<IType>();
            foreach (var module in Definition.Modules)
            {
                foreach (var typeDef in module.Types)
                {
                    results.Add(new ClrTypeDefinition(typeDef, this));
                }
            }
            return results;
        }

        private AttributeMap AnalyzeAttributes()
        {
            // TODO: actually analyze assembly attributes.
            return AttributeMap.Empty;
        }

        /// <summary>
        /// Runs a function in a single-threaded fashion with respect
        /// to other functions operating on this assembly.
        /// </summary>
        /// <param name="func">The function to run.</param>
        /// <typeparam name="T">
        /// The type of value produced by the function.
        /// </typeparam>
        /// <returns>
        /// The function's return value.
        /// </returns>
        public T RunSynchronized<T>(Func<T> func)
        {
            lock (SyncRoot)
            {
                return func();
            }
        }

        /// <summary>
        /// Runs a function in a single-threaded fashion with respect
        /// to other functions operating on this assembly.
        /// </summary>
        /// <param name="func">The function to run.</param>
        public void RunSynchronized(Action func)
        {
            lock (SyncRoot)
            {
                func();
            }
        }

        /// <summary>
        /// Creates a lazily initialized object from an initializer
        /// function that is run in a single-threaded fashion with
        /// respect to other functions operating on this assembly.
        /// </summary>
        /// <param name="func">
        /// The initialization function to run synchronously.
        /// </param>
        /// <typeparam name="T">
        /// The type of value to create.
        /// </typeparam>
        /// <returns>
        /// A lazily initialized object.
        /// </returns>
        public Lazy<T> CreateSynchronizedLazy<T>(Func<T> func)
        {
            return new Lazy<T>(() => RunSynchronized<T>(func));
        }

        /// <summary>
        /// Creates a deferred initializer object from an initializer
        /// function that is run in a single-threaded fashion with
        /// respect to other functions operating on this assembly.
        /// </summary>
        /// <param name="func">
        /// The initialization function to run synchronously.
        /// </param>
        /// <returns>
        /// A deferred initializer object.
        /// </returns>
        public DeferredInitializer CreateSynchronizedInitializer(Action func)
        {
            return DeferredInitializer.Create(() => RunSynchronized(func));
        }

        /// <summary>
        /// Resolves an assembly name reference as an assembly.
        /// </summary>
        /// <param name="assemblyRef">An assembly name reference to resolve.</param>
        /// <returns>The assembly referenced by <paramref name="assemblyRef"/>.</returns>
        public IAssembly Resolve(AssemblyNameReference assemblyRef)
        {
            return Resolver.Resolve(assemblyRef);
        }

        /// <summary>
        /// Resolves a type reference declared in this assembly.
        /// </summary>
        /// <param name="typeRef">The type reference to resolve.</param>
        /// <returns>A type referred to by the reference.</returns>
        public IType Resolve(TypeReference typeRef)
        {
            return Resolver.Resolve(typeRef, this);
        }

        /// <summary>
        /// Resolves a type reference declared in this assembly.
        /// </summary>
        /// <param name="typeRef">The type reference to resolve.</param>
        /// <param name="enclosingMember">
        /// The generic member that references a particular type. If non-null, type
        /// parameters are resolved from this member.
        /// </param>
        /// <returns>A type referred to by the reference.</returns>
        internal IType Resolve(TypeReference typeRef, IGenericMember enclosingMember)
        {
            return Resolver.Resolve(typeRef, this, enclosingMember);
        }

        /// <summary>
        /// Resolves a field reference declared in this assembly.
        /// </summary>
        /// <param name="fieldRef">The field reference to resolve.</param>
        /// <returns>A field referred to by the reference.</returns>
        public IField Resolve(FieldReference fieldRef)
        {
            return Resolver.Resolve(fieldRef, this);
        }

        /// <summary>
        /// Resolves a method reference declared in this assembly.
        /// </summary>
        /// <param name="methodRef">The method reference to resolve.</param>
        /// <returns>A method referred to by the reference.</returns>
        public IMethod Resolve(MethodReference methodRef)
        {
            return Resolver.Resolve(methodRef, this);
        }

        /// <summary>
        /// Resolves a property reference declared in this assembly.
        /// </summary>
        /// <param name="propertyRef">The property reference to resolve.</param>
        /// <returns>A property referred to by the reference.</returns>
        public IProperty Resolve(PropertyReference propertyRef)
        {
            return Resolver.Resolve(propertyRef, this);
        }

        /// <summary>
        /// Wraps a CIL assembly definition in a Flame assembly, setting
        /// up an assembly resolver and type system.
        /// </summary>
        /// <param name="definition">The assembly definition to wrap.</param>
        /// <returns>A Flame assembly.</returns>
        public static ClrAssembly Wrap(AssemblyDefinition definition)
        {
            // Bootstrap a type system resolver.
            var typeSystem = new MutableTypeEnvironment(null);
            var resolver = new CecilAssemblyResolver(
                definition.MainModule.AssemblyResolver,
                typeSystem);
            var flameAsm = new ClrAssembly(definition, resolver.ReferenceResolver);

            // Register the assembly with its own resolver, so we don't get weirdness
            // where the assembly tries to resolve itself.
            resolver.ReferenceResolver.Register(definition.Name, flameAsm);

            var objectType = flameAsm.Resolve(definition.MainModule.TypeSystem.Object);
            var corlib = objectType.Parent.Assembly;

            typeSystem.InnerEnvironment = new CorlibTypeEnvironment(corlib);

            return flameAsm;
        }
    }
}
