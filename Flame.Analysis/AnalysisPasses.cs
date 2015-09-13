using Flame.Compiler;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public static class AnalysisPasses
    {
        public static AnalysisPass Create(Func<IMethod, ICompilerLog, INodeVisitor> CreateVisitor)
        {
            return new AnalysisPass(CreateVisitor);
        }

        public static AnalysisPass Create(Func<ICompilerLog, INodeVisitor> CreateVisitor)
        {
            return Create((method, log) => CreateVisitor(log));
        }

        public static AnalysisPass ValueTypeDelegatePass
        {
            get
            {
                return Create(log => new ValueTypeDelegateVisitor(log));
            }
        }
    }
}
