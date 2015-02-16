using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public struct ReferenceDependency
    {
        public ReferenceDependency(string Identifier, bool UseCopy)
        {
            this = default(ReferenceDependency);
            this.Identifier = Identifier;
            this.UseCopy = UseCopy;
        }

        /// <summary>
        /// Gets the reference's identifier.
        /// </summary>
        public string Identifier { get; private set; }
        /// <summary>
        /// Gets a boolean value that indicates whether a copy of the assembly should be taken and used.
        /// </summary>
        public bool UseCopy { get; private set; }
    }
}
