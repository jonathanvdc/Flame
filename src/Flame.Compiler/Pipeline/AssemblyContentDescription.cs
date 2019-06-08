using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
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
        public static async Task<AssemblyContentDescription> CreateTransitiveAsync(
            QualifiedName fullName,
            AttributeMap attributes,
            IMethod entryPoint,
            Optimizer optimizer)
        {
            var types = ImmutableHashSet.CreateBuilder<IType>();
            var members = ImmutableHashSet.CreateBuilder<ITypeMember>();
            var bodies = new Dictionary<IMethod, MethodBody>();

            await AddToTransitiveAsync(entryPoint, types, members, bodies, optimizer);

            return new AssemblyContentDescription(
                fullName,
                attributes,
                types.ToImmutable(),
                members.ToImmutable(),
                bodies,
                entryPoint);
        }

        private static async Task AddToTransitiveAsync(
            IMethod method,
            ImmutableHashSet<IType>.Builder types,
            ImmutableHashSet<ITypeMember>.Builder members,
            Dictionary<IMethod, MethodBody> bodies,
            Optimizer optimizer)
        {
            if (!Define(method, types, members))
            {
                return;
            }

            // Add the method body itself.
            var body = await optimizer.GetBodyAsync(method);
            if (body == null)
            {
                return;
            }
            bodies[method] = body;
            Define(method, types, members);
            members.Add(method);

            var bodyMembers = body.Members;
            // Add field dependencies.
            foreach (var dependency in bodyMembers.OfType<IField>())
            {
                Define(dependency, types, members);
            }

            // Add type dependencies.
            var typeVisitor = new TypeFuncVisitor(t =>
            {
                if (!(t is ContainerType) && !(t is TypeSpecialization) && !(t is IGenericParameter))
                {
                    Define(t, types, members);
                }
                return t;
            });
            foreach (var dependency in bodyMembers.OfType<IType>())
            {
                typeVisitor.Visit(dependency);
            }

            // Add method dependencies.
            await optimizer.RunAllAsync(
                bodyMembers.OfType<IMethod>()
                    .Select(m => AddToTransitiveAsync(m, types, members, bodies, optimizer)));
        }

        private static bool Define(
            ITypeMember member,
            ImmutableHashSet<IType>.Builder types,
            ImmutableHashSet<ITypeMember>.Builder members)
        {
            if (members.Add(member))
            {
                if (member is IAccessor)
                {
                    members.Add(((IAccessor)member).ParentProperty);
                }
                var parent = member.ParentType;
                if (parent != null)
                {
                    Define(parent, types, members);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void Define(
            IType type,
            ImmutableHashSet<IType>.Builder types,
            ImmutableHashSet<ITypeMember>.Builder members)
        {
            if (types.Add(type))
            {
                var parent = type.Parent;
                if (parent.IsMethod)
                {
                    Define(parent.Method, types, members);
                }
                else if (parent.IsType)
                {
                    Define(parent.Type, types, members);
                }
            }
        }
    }
}
