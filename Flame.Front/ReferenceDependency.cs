using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    /// <summary>
    /// Identifies a dependency on a referenced assembly.
    /// </summary>
    public struct ReferenceDependency
    {
        public ReferenceDependency(string Identifier, bool UseCopy)
        {
            this = default(ReferenceDependency);
            this.Identifier = new PathIdentifier(Identifier);
            this.UseCopy = UseCopy;
        }
        public ReferenceDependency(PathIdentifier Identifier, bool UseCopy)
        {
            this = default(ReferenceDependency);
            this.Identifier = Identifier;
            this.UseCopy = UseCopy;
        }

        /// <summary>
        /// Gets the reference's identifier.
        /// </summary>
        public PathIdentifier Identifier { get; private set; }
        /// <summary>
        /// Gets a boolean value that indicates whether a copy of the assembly should be taken and used.
        /// </summary>
        public bool UseCopy { get; private set; }
    }
}
