using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    /// <summary>
    /// Defines a mutable view of a dependency set for the Flame IR format.
    /// </summary>
    public class IRDependencyBuilder
    {
        /// <summary>
        /// Creates a new Flame IR dependency builder.
        /// </summary>
        public IRDependencyBuilder()
        {
            this.rtDepends = new HashSet<string>();
            this.libDepends = new HashSet<string>();
        }

        private HashSet<string> rtDepends;
        private HashSet<string> libDepends;

        /// <summary>
        /// Gets the set of runtime dependencies.
        /// </summary>
        public IEnumerable<string> RuntimeDependencies
        {
            get { return rtDepends; }
        }

        /// <summary>
        /// Gets the set of library dependencies.
        /// </summary>
        public IEnumerable<string> LibraryDependencies
        {
            get { return libDepends; }
        }

        /// <summary>
        /// Gets the set of runtime dependency nodes.
        /// </summary>
        public IEnumerable<LNode> RuntimeDependencyNodes
        {
            get { return RuntimeDependencies.Select(item => NodeFactory.Call(IRParser.RuntimeDependencyNodeName, NodeFactory.Literal(item))); }
        }

        /// <summary>
        /// Gets the set of library dependency nodes.
        /// </summary>
        public IEnumerable<LNode> LibraryDependencyNodes
        {
            get { return LibraryDependencies.Select(item => NodeFactory.Call(IRParser.LibraryDependencyNodeName, NodeFactory.Literal(item))); }
        }

        /// <summary>
        /// Gets the set of all dependency nodes.
        /// </summary>
        public IEnumerable<LNode> DependencyNodes
        {
            get { return RuntimeDependencyNodes.Concat(LibraryDependencyNodes); }
        }

        /// <summary>
        /// Adds a runtime dependency to the dependency set.
        /// </summary>
        /// <param name="Name"></param>
        public void AddRuntimeDependency(string Name)
        {
            rtDepends.Add(Name);
        }

        /// <summary>
        /// Adds a library dependency to the dependency set.
        /// </summary>
        /// <param name="Name"></param>
        public void AddLibraryDependency(string Name)
        {
            libDepends.Add(Name);            
        }

        /// <summary>
        /// Adds a dependency to the dependency set.
        /// Unless the given name is a known runtime dependency,
        /// the given name is registered as a library dependency.
        /// </summary>
        /// <param name="Name"></param>
        public void AddDependency(string Name)
        {
            if (!rtDepends.Contains(Name))
            {
                libDepends.Add(Name);
            }
        }
    }
}
