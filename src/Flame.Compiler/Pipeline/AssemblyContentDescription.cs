using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Flame.Collections;
using Flame.TypeSystem;

namespace Flame.Compiler.Pipeline
{
    /// <summary>
    /// A description of a target assembly's contents.
    /// </summary>
    public sealed class AssemblyContentDescription
    {
        /// <summary>
        /// Creates an assembly content description.
        /// </summary>
        /// <param name="fullName">
        /// The assembly's full name.
        /// </param>
        /// <param name="attributes">
        /// The assembly's attribute map.
        /// </param>
        /// <param name="types">
        /// The list of types to include in the assembly.
        /// </param>
        /// <param name="typeMembers">
        /// The type members to include in the assembly.
        /// </param>
        /// <param name="methodBodies">
        /// A mapping of methods to method bodies.
        /// </param>
        /// <param name="entryPoint">
        /// An optional entry point method. <c>null</c> means that
        /// the assembly has no entry point.
        /// </param>
        public AssemblyContentDescription(
            QualifiedName fullName,
            AttributeMap attributes,
            ImmutableHashSet<IType> types,
            ImmutableHashSet<ITypeMember> typeMembers,
            IReadOnlyDictionary<IMethod, MethodBody> methodBodies,
            IMethod entryPoint)
        {
            this.FullName = fullName;
            this.Attributes = attributes;
            this.Types = types;
            this.TypeMembers = typeMembers;
            this.MethodBodies = methodBodies;
            this.EntryPoint = entryPoint;
        }

        /// <summary>
        /// Gets the full name of the assembly to build.
        /// </summary>
        /// <returns>A fully qualified name.</returns>
        public QualifiedName FullName { get; private set; }

        /// <summary>
        /// Gets the attribute map for the assembly to build.
        /// </summary>
        /// <returns>An attribute map.</returns>
        public AttributeMap Attributes { get; private set; }

        /// <summary>
        /// Gets the set of types to compile and include in
        /// the assembly.
        /// </summary>
        /// <returns>A set of types.</returns>
        public ImmutableHashSet<IType> Types { get; private set; }

        /// <summary>
        /// Gets the set of type members to compile and include
        /// in the assembly.
        /// </summary>
        /// <returns>A set of type members.</returns>
        public ImmutableHashSet<ITypeMember> TypeMembers { get; private set; }

        /// <summary>
        /// Gets a dictionary that maps method in this assembly to
        /// their method bodies.
        /// </summary>
        /// <returns>The method bodies dictionary.</returns>
        public IReadOnlyDictionary<IMethod, MethodBody> MethodBodies { get; private set; }

        /// <summary>
        /// Gets the assembly's entry point. Returns <c>null</c> if the
        /// assembly has no entry point.
        /// </summary>
        /// <returns>The assembly's entry point.</returns>
        public IMethod EntryPoint { get; private set; }

        /// <summary>
        /// Creates an assembly content description that transitively includes all
        /// dependencies for an entry point method.
        /// </summary>
        /// <param name="fullName">The name of the assembly.</param>
        /// <param name="attributes">The assembly's attributes.</param>
        /// <param name="entryPoint">An entry point method.</param>
        /// <param name="optimizer">An optimizer for method bodies.</param>
        /// <returns>An assembly content description.</returns>
        public static Task<AssemblyContentDescription> CreateTransitiveAsync(
            QualifiedName fullName,
            AttributeMap attributes,
            IMethod entryPoint,
            Optimizer optimizer)
        {
            return CreateTransitiveAsync(fullName, attributes, entryPoint, Enumerable.Empty<ITypeMember>(), optimizer);
        }

        /// <summary>
        /// Creates an assembly content description that contains a number of
        /// root members plus an optional entry point method. Dependencies
        /// are transitively included.
        /// </summary>
        /// <param name="fullName">The name of the assembly.</param>
        /// <param name="attributes">The assembly's attributes.</param>
        /// <param name="entryPoint">An entry point method. Specify <c>null</c> to have no entry point.</param>
        /// <param name="roots">A sequence of additional roots that must be included in the assembly.</param>
        /// <param name="optimizer">An optimizer for method bodies.</param>
        /// <returns>An assembly content description.</returns>
        public static Task<AssemblyContentDescription> CreateTransitiveAsync(
            QualifiedName fullName,
            AttributeMap attributes,
            IMethod entryPoint,
            IEnumerable<ITypeMember> roots,
            Optimizer optimizer)
        {
            return CreateTransitiveAsync(fullName, attributes, entryPoint, roots, EmptyArray<IType>.Value, optimizer);
        }

        /// <summary>
        /// Creates an assembly content description that contains a number of
        /// root members plus an optional entry point method. Dependencies
        /// are transitively included.
        /// </summary>
        /// <param name="fullName">The name of the assembly.</param>
        /// <param name="attributes">The assembly's attributes.</param>
        /// <param name="entryPoint">An entry point method. Specify <c>null</c> to have no entry point.</param>
        /// <param name="memberRoots">A sequence of additional root members that must be included in the assembly.</param>
        /// <param name="typeRoots">A sequence of additional root types that must be included in the assembly.</param>
        /// <param name="optimizer">An optimizer for method bodies.</param>
        /// <returns>An assembly content description.</returns>
        public static async Task<AssemblyContentDescription> CreateTransitiveAsync(
            QualifiedName fullName,
            AttributeMap attributes,
            IMethod entryPoint,
            IEnumerable<ITypeMember> memberRoots,
            IEnumerable<IType> typeRoots,
            Optimizer optimizer)
        {
            var builder = new TransitiveDescriptionBuilder(optimizer);
            foreach (var item in memberRoots)
            {
                await builder.DefineAsync(item);
            }
            foreach (var item in typeRoots)
            {
                await builder.DefineAsync(item);
            }
            if (entryPoint != null)
            {
                await builder.DefineAsync(entryPoint);
            }

            return new AssemblyContentDescription(
                fullName,
                attributes,
                builder.Types.ToImmutable(),
                builder.Members.ToImmutable(),
                builder.Bodies,
                entryPoint);
        }

        /// <summary>
        /// A data structure that helps construct assembly content descriptions
        /// for transitive dependencies.
        /// </summary>
        private struct TransitiveDescriptionBuilder
        {
            public TransitiveDescriptionBuilder(Optimizer optimizer)
            {
                this.Optimizer = optimizer;

                this.Types = ImmutableHashSet.CreateBuilder<IType>();
                this.Members = ImmutableHashSet.CreateBuilder<ITypeMember>();
                this.Bodies = new Dictionary<IMethod, MethodBody>();
                this.overrides = new Dictionary<IMethod, HashSet<IMethod>>();
                this.syncRoot = new object();
            }

            public ImmutableHashSet<IType>.Builder Types { get; private set; }
            public ImmutableHashSet<ITypeMember>.Builder Members { get; private set; }
            public Dictionary<IMethod, MethodBody> Bodies { get; private set; }
            public Optimizer Optimizer { get; private set; }

            private object syncRoot;

            private Dictionary<IMethod, HashSet<IMethod>> overrides;

            public async Task<bool> DefineAsync(ITypeMember member)
            {
                bool added;
                lock (syncRoot)
                {
                    added = Members.Add(member);
                }
                if (added)
                {
                    if (member is IAccessor)
                    {
                        lock (syncRoot)
                        {
                            Members.Add(((IAccessor)member).ParentProperty);
                        }
                    }
                    var parent = member.ParentType;
                    if (parent != null)
                    {
                        await DefineAsync(parent);
                    }
                    if (member is IMethod)
                    {
                        var method = (IMethod)member;
                        HashSet<IMethod> extraDefs;
                        lock (syncRoot)
                        {
                            if (!overrides.TryGetValue(method, out extraDefs))
                            {
                                extraDefs = null;
                            }
                        }
                        if (extraDefs != null)
                        {
                            await Optimizer.RunAllAsync(extraDefs.Select(DefineAsync));
                        }
                        await DefineBodyAsync(method);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }

            private Task RegisterOverridesAsync(IType type)
            {
                return Optimizer.RunAllAsync(
                    type.Methods
                        .Concat(type.Properties.SelectMany(p => p.Accessors))
                        .Select(RegisterOverridesAsync));
            }

            private async Task RegisterOverridesAsync(IMethod method)
            {
                bool anyDef = false;
                lock (syncRoot)
                {
                    foreach (var baseMethod in method.BaseMethods)
                    {
                        if (Members.Contains(baseMethod))
                        {
                            // One of this method's base methods defines this method.
                            // Break now and just define the method.
                            anyDef = true;
                            break;
                        }

                        // Add this method to the override set of every base method. If
                        // and when the base method is defined, it will also define all
                        // methods in the override set.
                        HashSet<IMethod> overrideSet;
                        if (!overrides.TryGetValue(baseMethod, out overrideSet))
                        {
                            overrides[baseMethod] = overrideSet = new HashSet<IMethod>();
                        }
                        overrideSet.Add(method);
                    }
                }
                if (anyDef)
                {
                    await DefineAsync(method);
                }
            }

            public async Task<bool> DefineAsync(IType type)
            {
                bool added;
                lock (syncRoot)
                {
                    added = Types.Add(type);
                }
                if (added)
                {
                    // Define the type's parent method or type.
                    var parent = type.Parent;
                    if (parent.IsMethod)
                    {
                        await DefineAsync(parent.Method);
                    }
                    else if (parent.IsType)
                    {
                        await DefineAsync(parent.Type);
                    }

                    // Register the type's overrides.
                    await RegisterOverridesAsync(type);

                    // Define the type's static constructors, if it defines any.
                    // This ensures that any initialization logic is performed
                    // correctly.
                    foreach (var method in type.Methods)
                    {
                        if (method.IsStatic && method.IsConstructor)
                        {
                            await DefineAsync(method);
                        }
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }

            private async Task DefineBodyAsync(IMethod method)
            {
                // Add the method body itself.
                var body = await Optimizer.GetBodyAsync(method);
                if (body == null)
                {
                    return;
                }
                lock (syncRoot)
                {
                    Bodies[method] = body;
                }

                var bodyMembers = body.Members;
                // Add field dependencies.
                foreach (var dependency in bodyMembers.OfType<IField>())
                {
                    await DefineAsync(dependency);
                }

                // Add type dependencies.
                var typeDeps = new List<IType>();
                var typeVisitor = new TypeFuncVisitor(t =>
                {
                    if (!(t is ContainerType) && !(t is TypeSpecialization) && !(t is IGenericParameter))
                    {
                        typeDeps.Add(t);
                    }
                    return t;
                });
                foreach (var dependency in bodyMembers.OfType<IType>())
                {
                    typeVisitor.Visit(dependency);
                }
                await Optimizer.RunAllAsync(typeDeps.Select(DefineAsync));

                // Add method dependencies.
                await Optimizer.RunAllAsync(
                    bodyMembers
                        .OfType<IMethod>()
                        .Select(DefineAsync));
            }
        }
    }
}
