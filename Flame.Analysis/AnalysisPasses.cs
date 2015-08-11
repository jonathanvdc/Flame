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
        public static AnalysisPass Create(INodeVisitor Visitor)
        {
            return new AnalysisPass(Visitor);
        }

        public static AnalysisPass CreateValueTypeDelegatePass(ICompilerLog Log)
        {
            return Create(new ValueTypeDelegateVisitor(Log));
        }
    }
}
