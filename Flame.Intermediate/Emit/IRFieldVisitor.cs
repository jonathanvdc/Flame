using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRFieldVisitor : IConverter<IField, LNode>
    {
        public IRFieldVisitor(IRAssemblyBuilder Assembly)
        {
            this.Assembly = Assembly;
        }

        public IRAssemblyBuilder Assembly { get; private set; }

        public LNode Convert(IField Value)
        {
            if (Value is GenericInstanceField)
            {
                return NodeFactory.Call(IRParser.GenericInstanceMemberName, new LNode[]
                { 
                    Assembly.TypeTable.GetReference(Value.DeclaringType), 
                    Assembly.FieldTable.GetReference(((GenericInstanceField)Value).Declaration) 
                });
            }
            else
            {
                return NodeFactory.Call(IRParser.FieldReferenceName, new LNode[]
                {
                    Assembly.TypeTable.GetReference(Value.DeclaringType), 
                    NodeFactory.Literal(Value.Name),
                    NodeFactory.Literal(Value.IsStatic)
                });
            }
        }
    }
}
