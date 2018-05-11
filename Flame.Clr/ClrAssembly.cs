using System.Collections.Generic;
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
        public ClrAssembly(AssemblyDefinition definition)
        {
            this.Definition = definition;
            this.FullName = new SimpleName(definition.Name.Name).Qualify();
        }

        /// <summary>
        /// Gets the Cecil assembly definition wrapped by this assembly.
        /// </summary>
        /// <returns>A Cecil assembly definition.</returns>
        public AssemblyDefinition Definition { get; private set; }

        /// <inheritdoc/>
        public UnqualifiedName Name => FullName.FullyUnqualifiedName;

        /// <inheritdoc/>
        public QualifiedName FullName { get; private set; }

        /// <inheritdoc/>
        public AttributeMap Attributes => throw new System.NotImplementedException();

        /// <inheritdoc/>
        public IReadOnlyList<IType> Types => throw new System.NotImplementedException();
    }
}