using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Loyc;
using Loyc.Collections;
using Loyc.Syntax;
using Pixie;

namespace Flame.Intermediate
{
    /// <summary>
    /// A collection of methods that convert Pixie markup nodes to and from LNodes.
    /// </summary>
    public static class MarkupHelpers
    {
        /// <summary>
        /// Serializes the given markup node as an LNode.
        /// </summary>
        /// <param name="Node">The markup node to serialize.</param>
        /// <returns>An equivalent LNode.</returns>
        public static LNode Serialize(MarkupNode Node)
        {
            var attrList = new VList<LNode>();
            foreach (var key in Node.Attributes.Keys)
            {
                string val = Node.Attributes.Get<string>(key, null);

                if (val != null)
                {
                    attrList.Add(LNode.Call(
                        CodeSymbols.Assign,
                        new VList<LNode>()
                        {
                            LNode.Id(GSymbol.Get(key)),
                            LNode.Literal(val)
                        }));
                }
            }

            return LNode.Call(
                attrList,
                GSymbol.Get(Node.Type),
                new VList<LNode>(Node.Children.Select(Serialize)));
        }

        /// <summary>
        /// Serializes the given LNode as a markup node.
        /// </summary>
        /// <param name="Node">The LNode to deserialize.</param>
        /// <returns>An equivalent markup node.</returns>
        public static MarkupNode Deserialize(LNode Node)
        {
            var attrDict = new Dictionary<string, object>();
            foreach (var attr in Node.Attrs)
            {
                if (!attr.Calls(CodeSymbols.Assign, 2))
                {
                    Debug.Assert(false, "LNode attribute is not a binary assignment call.");
                    continue;
                }

                if (!attr.Args[0].IsId)
                {
                    Debug.Assert(false, "LNode attribute left-hand side is not an identifier.");
                    continue;
                }

                if (!attr.Args[1].IsLiteral)
                {
                    Debug.Assert(false, "LNode attribute right-hand side is not a literal.");
                    continue;
                }

                attrDict[attr.Args[0].Name.Name] = attr.Args[1].Value;
            }

            return new MarkupNode(
                Node.Name.Name,
                new PredefinedAttributes(attrDict),
                Node.Args.Select(Deserialize).ToArray());
        }
    }
}
