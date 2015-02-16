using Flame.Recompilation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Options
{
    public class OptimizerOption : IBuildOption<IMethodOptimizer>
    {
        public IMethodOptimizer GetValue(string[] Input)
        {
            switch (Input.Single())
            {
                case "analysis":
                case "aggressive":
                    return new AnalyzingOptimizer();
                case "default":
                case "conservative":
                default:
                    return new DefaultOptimizer();
            }
        }

        public string Key
        {
            get { return "optimize"; }
        }

        public int ArgumentsCount
        {
            get { return 1; }
        }
    }
}
