using System;
using System.Collections.Generic;

namespace Flame.Collections
{
    /// <summary>
    /// Defines algorithms that compute strongly connected components.
    /// </summary>
    public static class StronglyConnectedComponents
    {
        /// <summary>
        /// Computes all strongly connected components in a graph.
        /// </summary>
        /// <param name="vertices">The vertices in a graph.</param>
        /// <param name="getSuccessors">
        /// A function that takes a vertex in a graph and computes its successors.
        /// </param>
        /// <typeparam name="T">
        /// The type of a vertex.
        /// </typeparam>
        /// <returns>
        /// A list of strongly connected components.
        /// </returns>
        public static IReadOnlyList<HashSet<T>> Compute<T>(
            IEnumerable<T> vertices,
            Func<T, IEnumerable<T>> getSuccessors)
        {
            // An implementation of Tarjan's Strongly Connected Components algorithm.

            int index = 0;
            var vertexIndices = new Dictionary<T, int>();
            var vertexLowlinks = new Dictionary<T, int>();
            var stack = new Stack<T>();
            var onStack = new HashSet<T>();
            var sccs = new List<HashSet<T>>();
            foreach (var v in vertices)
            {
                if (!vertexIndices.ContainsKey(v))
                {
                    StrongConnect(v, getSuccessors, ref index, vertexIndices, vertexLowlinks, stack, onStack, sccs);
                }
            }

            return sccs;
        }

        private static void StrongConnect<T>(
            T v,
            Func<T, IEnumerable<T>> getSuccessors,
            ref int index,
            Dictionary<T, int> vertexIndices,
            Dictionary<T, int> vertexLowlinks,
            Stack<T> stack,
            HashSet<T> onStack,
            List<HashSet<T>> sccs)
        {
            // Set the depth index for v to the smallest unused index.
            vertexIndices[v] = index;
            vertexLowlinks[v] = index;
            index++;
            stack.Push(v);
            onStack.Add(v);

            // Consider successors of v.
            foreach (var w in getSuccessors(v))
            {
                int wIndex;
                if (!vertexIndices.TryGetValue(w, out wIndex))
                {
                    // Successor w has not yet been visited; recurse on it.
                    StrongConnect(w, getSuccessors, ref index, vertexIndices, vertexLowlinks, stack, onStack, sccs);
                    vertexLowlinks[v] = Math.Min(vertexLowlinks[v], vertexLowlinks[w]);
                }
                else if (onStack.Contains(w))
                {
                    // Successor w is in stack S and hence in the current SCC
                    // If w is not on stack, then (v, w) is a cross-edge in the DFS tree and must be ignored
                    // Note: The next line may look odd - but is correct.
                    // It says w.index not w.lowlink; that is deliberate and from the original paper.
                    vertexLowlinks[v] = Math.Min(vertexLowlinks[v], vertexIndices[w]);
                }
            }

            // If v is a root node, pop the stack and generate an SCC.
            if (vertexLowlinks[v] == vertexIndices[v])
            {
                var newScc = new HashSet<T>();
                T w;
                do
                {
                    w = stack.Pop();
                    onStack.Remove(w);
                    newScc.Add(w);
                } while (!object.Equals(w, v));
                sccs.Add(newScc);
            }
        }
    }
}
