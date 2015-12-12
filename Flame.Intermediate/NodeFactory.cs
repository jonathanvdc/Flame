using Loyc;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public static class NodeFactory
    {
        static NodeFactory()
        {
            factory = new LNodeFactory(new EmptySourceFile("<synthetic>"));
        }

        private static LNodeFactory factory;

        public static LNode Id(string Name)
        {
            return factory.Id(Name);
        }

        public static LNode IdOrLiteral(string Name)
        {
            if (string.IsNullOrEmpty(Name))
            {
                return Literal(Name);
            }
            else
            {
                return Id(Name);
            }
        }

        public static LNode Call(string Target, IEnumerable<LNode> Arguments)
        {
            return factory.Call(GSymbol.Get(Target), Arguments);
        }

        public static LNode Call(LNode Target, IEnumerable<LNode> Arguments)
        {
            return factory.Call(Target, Arguments);
        }

        public static LNode Literal(object Value)
        {
            return factory.Literal(Value);
        }

		#region VarLiteral

		/// <summary>
		/// Creates a literal node whose encoding varies
		/// based on its value.
		/// </summary>
		/// <returns>A literal node.</returns>
		/// <param name="Value">The value to encode.</param>
		public static LNode VarLiteral(short Value)
		{
			if (Value < sbyte.MaxValue && Value > sbyte.MinValue)
			{
				return Literal((sbyte)Value);
			}
			else
			{
				return factory.Literal(Value);
			}
		}

		/// <summary>
		/// Creates a literal node whose encoding varies
		/// based on its value.
		/// </summary>
		/// <returns>A literal node.</returns>
		/// <param name="Value">The value to encode.</param>
		public static LNode VarLiteral(int Value)
		{
			if (Value < short.MaxValue && Value > short.MinValue)
			{
				return VarLiteral((short)Value);
			}
			else
			{
				return factory.Literal(Value);
			}
		}

		/// <summary>
		/// Creates a literal node whose encoding varies
		/// based on its value.
		/// </summary>
		/// <returns>A literal node.</returns>
		/// <param name="Value">The value to encode.</param>
		public static LNode VarLiteral(long Value)
		{
			if (Value < int.MaxValue && Value > int.MinValue)
			{
				return VarLiteral((int)Value);
			}
			else
			{
				return factory.Literal(Value);
			}
		}

		#endregion

        public static LNode Block(IEnumerable<LNode> Arguments)
        {
            var node = factory.Braces(Arguments);
            if (node.Args.IsEmpty)
            {
                return node;
            }
            else
            {
                return node.WithStyle(NodeStyle.Statement);
            }
        }

        public static bool IsBlock(LNode Node)
        {
            return Node.Name.Name == CodeSymbols.Braces.Name;
        }

        /// <summary>
        /// "Unpacks" the given node:
        /// if it is a block node, its argument
        /// list is returned. Otherwise,
        /// the node itself is returned.
        /// </summary>
        /// <param name="Node"></param>
        /// <returns></returns>
        public static IEnumerable<LNode> UnpackBlock(LNode Node)
        {
            if (IsBlock(Node))
	        {
		        return Node.Args;
	        }
            else
            {
                return new LNode[] { Node };
            }
        }

        /// <summary>
        /// Packs the given nodes into a single block node. 
        /// If one or both of the arguments is a block node, 
        /// it is unpacked and its contents are added to the 
        /// resulting block node.
        /// </summary>
        /// <param name="Left"></param>
        /// <param name="Right"></param>
        /// <returns></returns>
        public static LNode MergedBlock(LNode Left, LNode Right)
        {
            return Block(UnpackBlock(Left).Concat(UnpackBlock(Right)));
        }
    }
}
