using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRGenericMemberTypeVisitor : IRTypeVisitor
    {
        public IRGenericMemberTypeVisitor(IRAssemblyBuilder Assembly, IGenericMember GenericMember)
            : base(Assembly)
        {
            this.GenericMember = GenericMember;
        }

        public IGenericMember GenericMember { get; private set; }

        protected override LNode ConvertGenericParameter(IGenericParameter Type)
        {
            if (Type.DeclaringMember.Equals(GenericMember))
            {
                int index = 0;
                foreach (var item in GenericMember.GenericParameters)
	            {
		            if (item.IsEquivalent(Type))
                    {
                        return NodeFactory.Call(IRParser.LocalGenericParameterReferenceName, new LNode[]
                        {
                            NodeFactory.Literal(index)
                        });
                    }
                    index++;
	            }
            }
            return base.ConvertGenericParameter(Type);
        }
    }
}
