using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRMethodVisitor : IConverter<IMethod, LNode>
    {
        public IRMethodVisitor(IRAssemblyBuilder Assembly)
        {
            this.Assembly = Assembly;
        }

        public IRAssemblyBuilder Assembly { get; private set; }

        public LNode Convert(IMethod Value)
        {
            if (Value is GenericMethod)
            {
                var genInst = (GenericMethod)Value;
                return NodeFactory.Call(IRParser.GenericInstanceName, new LNode[]
                { 
                    Assembly.MethodTable.GetReference(genInst.Declaration)
                }.Concat(genInst.GenericArguments.Select(Assembly.TypeTable.GetReference)));
            }
            else if (Value is GenericInstanceMethod)
            {
                return NodeFactory.Call(IRParser.GenericInstanceMemberName, new LNode[]
                { 
                    Assembly.TypeTable.GetReference(Value.DeclaringType), 
                    Assembly.MethodTable.GetReference(((GenericInstanceMethod)Value).Declaration)
                });
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
