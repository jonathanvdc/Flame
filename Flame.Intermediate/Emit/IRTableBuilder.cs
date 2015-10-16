using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    /// <summary>
    /// Defines a mutable view of an IR table that behaves like an ordered set.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class IRTableBuilder<T> : INodeStructure<IReadOnlyList<T>>
    {
        public IRTableBuilder(string TableName, Func<T, LNode> CreateNode, Func<int, LNode> CreateReferenceNode)
        {
            this.TableName = TableName;
            this.CreateElementNode = CreateNode;
            this.CreateReferenceNode = CreateReferenceNode;
            this.nodes = new List<LNode>();
            this.items = new List<T>();
            this.mappedItems = new Dictionary<T, int>();
        }

        /// <summary>
        /// Gets the table's name.
        /// </summary>
        public string TableName { get; private set; }

        /// <summary>
        /// Creates a node for the given element.
        /// </summary>
        public Func<T, LNode> CreateElementNode { get; private set; }

        /// <summary>
        /// Creates a node that represents a reference 
        /// to the nth element in the table.
        /// </summary>
        public Func<int, LNode> CreateReferenceNode { get; private set; }

        private List<LNode> nodes;
        private List<T> items;
        private Dictionary<T, int> mappedItems;

        /// <summary>
        /// Gets a boolean value that indicates whether this table contains
        /// the given element.
        /// </summary>
        /// <param name="Element"></param>
        /// <returns></returns>
        public bool Contains(T Element)
        {
            return mappedItems.ContainsKey(Element);
        }

        /// <summary>
        /// Gets the given element's index in this table.
        /// If this table contains no entry matching the given element,
        /// a new node is created by the given element creation
        /// delegate and added to the table.
        /// </summary>
        /// <param name="Element"></param>
        /// <param name="CreateElementNode"></param>
        /// <returns></returns>
        public int GetIndex(T Element, Func<T, LNode> CreateElementNode)
        {
            int result;
            if (mappedItems.TryGetValue(Element, out result))
            {
                return result;
            }
            else
            {
                int index = nodes.Count;
                nodes.Add(CreateElementNode(Element));
                items.Add(Element);
                mappedItems[Element] = index;
                return index;
            }
        }

        /// <summary>
        /// Gets the given element's index in this table.
        /// If this table contains no entry matching the given element,
        /// a new node is created and added to the table.
        /// </summary>
        /// <param name="Element"></param>
        /// <returns></returns>
        public int GetIndex(T Element)
        {
            return GetIndex(Element, CreateElementNode);
        }

        /// <summary>
        /// Gets a reference to the given element in the table.
        /// If this table contains no entry matching the given element,
        /// a new node is created and added to the table.
        /// </summary>
        /// <param name="Element"></param>
        /// <returns></returns>
        public LNode GetReference(T Element)
        {
            return CreateReferenceNode(GetIndex(Element));
        }

        /// <summary>
        /// Gets a reference to the given element in the table
        /// and wraps it in a node structure.
        /// If this table contains no entry matching the given element,
        /// a new node is created and added to the table.
        /// </summary>
        /// <param name="Element"></param>
        /// <returns></returns>
        public INodeStructure<T> GetReferenceStructure(T Element)
        {
            return new ConstantNodeStructure<T>(GetReference(Element), Element);
        }
        
        public LNode Node
        {
            get
            {
                return NodeFactory.Call(TableName, nodes);
            }
        }

        public IReadOnlyList<T> Value
        {
            get { return items; }
        }
    }
}
