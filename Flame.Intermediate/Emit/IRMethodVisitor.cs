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
            throw new NotImplementedException();
        }
    }
}
