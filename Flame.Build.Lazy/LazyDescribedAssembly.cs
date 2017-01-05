using System;
using Flame.Binding;
using System.Collections.Generic;

namespace Flame.Build.Lazy
{
    /// <summary>
    /// A type of assembly that constructs itself lazily in an imperative
    /// fashion.
    /// </summary>
    public sealed class LazyDescribedAssembly :
        LazyDescribedMember, IAssembly, INamespaceBranch
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="Flame.Build.Lazy.LazyDescribedAssembly"/> class.
        /// </summary>
        /// <param name="Name">The assembly's name.</param>
        /// <param name="Environment">The assembly's environment.</param>
        /// <param name="Initialize">
        /// An action that initializes the assembly.
        /// </param>
        public LazyDescribedAssembly(
            string Name, IEnvironment Environment,
            Action<LazyDescribedAssembly> Initialize)
            : this(new SimpleName(Name), Environment, Initialize)
        { }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="Flame.Build.Lazy.LazyDescribedAssembly"/> class.
        /// </summary>
        /// <param name="Name">The assembly's name.</param>
        /// <param name="Environment">The assembly's environment.</param>
        /// <param name="Initialize">
        /// An action that initializes the assembly.
        /// </param>
        public LazyDescribedAssembly(
            UnqualifiedName Name, IEnvironment Environment,
            Action<LazyDescribedAssembly> Initialize)
            : base(Name)
        {
            this.Environment = Environment;
            this.initializer = new DeferredInitializer<LazyDescribedAssembly>(
                Initialize);
        }

        /// <summary>
        /// Gets this assembly's environment.
        /// </summary>
        /// <value>The environment for this assembly.</value>
        public IEnvironment Environment { get; private set; }

        private Version asmVersion;
        private IMethod asmEntry;
        private List<IType> types;
        private List<INamespaceBranch> nsBranches;

        private DeferredInitializer<LazyDescribedAssembly> initializer;

        /// <summary>
        /// Gets the assembly's version.
        /// </summary>
        /// <value>The assembly version.</value>
        public Version AssemblyVersion
        {
            get
            {
                CreateBody();
                return asmVersion;
            }
            set
            {
                CreateBody();
                asmVersion = value;
            }
        }

        /// <summary>
        /// Gets or sets the entry point.
        /// </summary>
        /// <value>The entry point.</value>
        public IMethod EntryPoint
        {
            get
            {
                CreateBody();
                return asmEntry;
            }
            set
            {
                CreateBody();
                asmEntry = value;
            }
        }

        /// <summary>
        /// Gets the top-level types that are declared in this assembly.
        /// </summary>
        /// <value>The types.</value>
        public IEnumerable<IType> Types
        {
            get
            {
                CreateBody();
                return types;
            }
        }

        /// <summary>
        /// Gets the top-level namespaces that are declared in this assembly.
        /// </summary>
        /// <value>The types.</value>
        public IEnumerable<INamespaceBranch> Namespaces
        {
            get
            {
                CreateBody();
                return nsBranches;
            }
        }

        /// <inheritdoc/>
        public override QualifiedName FullName
        {
            get
            {
                return Name.Qualify();
            }
        }

        /// <inheritdoc/>
        protected override void CreateBody()
        {
            initializer.Initialize(this);
        }

        /// <summary>
        /// Creates a binder for this assembly.
        /// </summary>
        /// <returns></returns>
        public IBinder CreateBinder()
        {
            return new NamespaceTreeBinder(Environment, this);
        }

        /// <summary>
        /// Gets the entry point method for this assembly.
        /// </summary>
        /// <returns></returns>
        public IMethod GetEntryPoint()
        {
            return EntryPoint;
        }

        /// <summary>
        /// Adds the given type to this assembly.
        /// </summary>
        /// <param name="Type">The type to register.</param>
        public void AddType(IType Type)
        {
            CreateBody();
            types.Add(Type);
        }

        /// <summary>
        /// Adds the given namespace to this assembly.
        /// </summary>
        /// <param name="Namespace">The namespace to register.</param>
        public void AddNamespace(INamespaceBranch Namespace)
        {
            CreateBody();
            nsBranches.Add(Namespace);
        }

        IAssembly INamespace.DeclaringAssembly
        {
            get { return this; }
        }
    }
}
