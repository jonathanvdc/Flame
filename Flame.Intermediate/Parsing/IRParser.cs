using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Parsing
{
    // IR file contents:
    // - Dependency set
    // - Type table
    // - Method table
    // - Field table
    // - Assembly

    public class IRParser
    {
        public const string DependencyNodeName = "#external_dependencies";

        /// <summary>
        /// Searches the given sequence of top-level nodes for the given table type,
        /// and returns all entries in the said table nodes, concatenated head to tail.
        /// </summary>
        /// <param name="Nodes"></param>
        /// <param name="TableType"></param>
        /// <returns></returns>
        public static IEnumerable<LNode> GetTableEntries(IEnumerable<LNode> Nodes, string TableType)
        {
            return Nodes.Where(item => item.Name.Name == TableType)
                        .SelectMany(item => item.Args);
        }

        /// <summary>
        /// Parses a table of some kind. Said table is defined
        /// by the concatenation of all nodes of a specific type in the given sequence of nodes.
        /// Each item is parsed by the given parsing function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Nodes">The sequence of nodes to search for the given type of table.</param>
        /// <param name="TableType">The identifier for the type of table to parse.</param>
        /// <param name="ParseEntry">The parsing function for a table entry.</param>
        /// <returns></returns>
        public static IReadOnlyList<T> ParseTable<T>(IEnumerable<LNode> Nodes, string TableType, Func<LNode, T> ParseEntry)
        {
            return GetTableEntries(Nodes, TableType).Select(ParseEntry)
                                                    .ToArray();
        }

        /// <summary>
        /// Parses a set of some kind. Said set is defined
        /// by the concatenation of all nodes of a specific type in the given sequence of nodes.
        /// Each item is parsed by the given parsing function.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="Nodes">The sequence of nodes to search for the given type of set.</param>
        /// <param name="TableType">The identifier for the type of set to parse.</param>
        /// <param name="ParseEntry">The parsing function for a set item.</param>
        /// <returns></returns>
        public static IEnumerable<T> ParseSet<T>(IEnumerable<LNode> Nodes, string TableType, Func<LNode, T> ParseEntry)
        {
            return GetTableEntries(Nodes, TableType).Select(ParseEntry)
                                                    .Distinct();
        }

        /// <summary>
        /// Parses all dependency tables in the given sequence of nodes.
        /// </summary>
        /// <param name="Nodes"></param>
        /// <returns></returns>
        public static IEnumerable<string> ParseDependencies(IEnumerable<LNode> Nodes)
        {
            return ParseSet(Nodes, DependencyNodeName, item => item.Name.Name);
        }
    }
}
