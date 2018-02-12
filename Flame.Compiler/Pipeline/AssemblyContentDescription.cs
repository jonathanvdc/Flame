using System.Collections.Generic;
using System.Collections.Immutable;

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
        /// <param name="fullName"></param>
        /// <param name="attributes"></param>
        /// <param name="types"></param>
        /// <param name="typeMembers"></param>
        /// <param name="methodBodies"></param>
        /// <param name="entryPoint"></param>
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
    }
}