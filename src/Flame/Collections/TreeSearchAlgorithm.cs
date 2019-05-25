using System;
using System.Collections.Generic;

namespace Flame.Collections
{
    /// <summary>
    /// A base class for algorithms that search trees.
    /// </summary>
    /// <typeparam name="TInternalNode">
    /// The type of an internal tree node.
    /// </typeparam>
    /// <typeparam name="TLeafNode">
    /// The type of a leaf node.
    /// </typeparam>
    public abstract class TreeSearchAlgorithm<TInternalNode, TLeafNode>
    {
        /// <summary>
        /// Creates a tree search algorithm.
        /// </summary>
        /// <param name="getChildren">
        /// A function that takes an internal node and produces its children.
        /// </param>
        public TreeSearchAlgorithm(
            Func<TInternalNode, Tuple<IEnumerable<TInternalNode>, IEnumerable<TLeafNode>>> getChildren)
        {
            this.GetChildren = getChildren;
        }

        /// <summary>
        /// Gets the children of an internal node as an
        /// (internal nodes, leaf nodes) pair.
        /// </summary>
        public Func<TInternalNode, Tuple<IEnumerable<TInternalNode>, IEnumerable<TLeafNode>>> GetChildren { get; private set; }

        /// <summary>
        /// Searches through a tree rooted at a particular node.
        /// </summary>
        /// <param name="root">The root of the tree to search through.</param>
        /// <returns>A leaf node.</returns>
        public abstract TLeafNode Search(TInternalNode root);
    }
}
