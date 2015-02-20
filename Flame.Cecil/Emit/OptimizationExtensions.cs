using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public static class OptimizationExtensions
    {
        public static bool ApplyAnyOptimization(this IEmitContext Context, params IPeepholeOptimization[] Optimizations)
        {
            return Context.ApplyAnyOptimization((IEnumerable<IPeepholeOptimization>)Optimizations);
        }

        public static bool ApplyAnyOptimization(this IEmitContext Context, IEnumerable<IPeepholeOptimization> Optimizations)
        {
            var ordered = Optimizations.OrderByDescending((item) => item.InstructionCount); // Try big optimizations first
            foreach (var item in ordered)
            {
                if (Context.ApplyOptimization(item))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
