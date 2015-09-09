using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class DissectedCall
    {
        public DissectedCall(IExpression ThisValue, IMethod Method, IEnumerable<IExpression> Arguments, bool IsVirtual)
        {
            this.ThisValue = ThisValue;
            this.Method = Method;
            this.Arguments = Arguments;
            this.IsVirtual = IsVirtual;
        }

        public IExpression ThisValue { get; private set; }
        public IMethod Method { get; private set; }
        public IEnumerable<IExpression> Arguments { get; private set; }
        public bool IsVirtual { get; private set; }
    }

}
