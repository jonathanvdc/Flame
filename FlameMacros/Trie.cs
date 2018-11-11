using System.Collections.Generic;

namespace FlameMacros
{
    /// <summary>
    /// A generic trie node.
    /// </summary>
    /// <typeparam name="TKeyElement">The type of key.</typeparam>
    /// <typeparam name="TValue">The type of value.</typeparam>
    public sealed class TrieNode<TKeyElement, TValue>
    {
        public TrieNode()
            : this(default(TValue))
        { }

        public TrieNode(TValue value)
        {
            this.Value = value;
            this.childNodes = new Dictionary<TKeyElement, TrieNode<TKeyElement, TValue>>();
        }

        /// <summary>
        /// Gets the value for this trie element node, if any.
        /// </summary>
        /// <value>The value for this trie node.</value>
        public TValue Value { get; set; }

        private Dictionary<TKeyElement, TrieNode<TKeyElement, TValue>> childNodes;

        /// <summary>
        /// Gets the trie node's children.
        /// </summary>
        /// <value>A mapping of key elements to children.</value>
        public IReadOnlyDictionary<TKeyElement, TrieNode<TKeyElement, TValue>> Children => childNodes;

        /// <summary>
        /// Tries to get the value assigned to a particular key.
        /// </summary>
        /// <param name="keyElements">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if a value has been assigned to the key; otherwise, <c>false</c>.</returns>
        public bool TryGetValue(IReadOnlyList<TKeyElement> keyElements, out TValue value)
        {
            return TryGetValue(keyElements, 0, out value);
        }

        private bool TryGetValue(IReadOnlyList<TKeyElement> keyElements, int offset, out TValue value)
        {
            if (keyElements.Count == offset)
            {
                value = this.Value;
                return true;
            }
            else
            {
                TrieNode<TKeyElement, TValue> child;
                if (childNodes.TryGetValue(keyElements[offset], out child))
                {
                    return child.TryGetValue(keyElements, offset + 1, out value);
                }
                else
                {
                    value = default(TValue);
                    return false;
                }
            }
        }

        /// <summary>
        /// Sets a key-value pair in the trie.
        /// </summary>
        /// <param name="keyElements">The key.</param>
        /// <param name="value">The value.</param>
        public void Set(IReadOnlyList<TKeyElement> keyElements, TValue value)
        {
            Set(keyElements, 0, value);
        }

        private void Set(IReadOnlyList<TKeyElement> keyElements, int offset, TValue value)
        {
            if (keyElements.Count == offset)
            {
                this.Value = value;
                return;
            }

            TrieNode<TKeyElement, TValue> child;
            if (!childNodes.TryGetValue(keyElements[offset], out child))
            {
                child = new TrieNode<TKeyElement, TValue>();
                childNodes[keyElements[offset]] = child;
            }
            child.Set(keyElements, offset + 1, value);
        }
    }
}
