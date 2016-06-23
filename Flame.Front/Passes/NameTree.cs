using System;
using System.Collections.Generic;
using System.Linq;

namespace Flame.Front.Passes
{
    /// <summary>
    /// A tree of names. This data structure can be useful
    /// when letting the user know which passes are in use.
    /// </summary>
    public class NameTree
    {
        public NameTree(string Name)
            : this(Name, new NameTree[] { })
        { }
        public NameTree(
            string Name, IReadOnlyList<NameTree> Children)
        {
            this.Name = Name;
            this.Children = Children;
        }

        /// <summary>
        /// Gets the tree node's name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets this tree node's children.
        /// </summary>
        public IReadOnlyList<NameTree> Children { get; private set; }

        /// <summary>
        /// Checks if this name node is a leaf node.
        /// </summary>
        /// <value><c>true</c> if this instance is a leaf node; otherwise, <c>false</c>.</value>
        public bool IsLeaf { get { return Children.Count == 0; } }

        /// <summary>
        /// Applies the given mapping function to this name tree.
        /// </summary>
        public NameTree Select(Func<string, string> Mapping)
        {
            return new NameTree(
                Mapping(Name), 
                Children.Select(
                    c => c.Select(Mapping)).ToArray());
        }

        /// <summary>
        /// Applies the given filtering function to this name tree.
        /// Null is returned if the root node does not match the 
        /// predicate.
        /// </summary>
        public NameTree Where(Func<string, bool> Predicate)
        {
            if (!Predicate(Name))
                return null;
            else
                return new NameTree(
                    Name, 
                    Children
                    .Select(c => c.Where(Predicate))
                    .Where(c => c != null)
                    .ToArray());
        }
    }
}

