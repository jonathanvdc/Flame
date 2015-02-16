using Flame;
using Flame.Analysis;
using Flame.Compiler;
using Flame.Recompilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public class AnalyzingOptimizer : IMethodOptimizer
    {
        public AnalyzingOptimizer()
        {

        }

        public IStatement GetOptimizedBody(IBodyMethod Method)
        {
            return Method.GetOptimizedBody();
        }

        public IEnumerable<IAttribute> InferAttributes(IMethod Method)
        {
            return Enumerable.Empty<IAttribute>();
        }
    }
}
