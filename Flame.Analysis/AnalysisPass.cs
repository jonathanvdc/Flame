using Flame.Compiler;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    /// <summary>
    /// A pass that does produces diagnostics, but does not modify its input.
    /// </summary>
    public class AnalysisPass : IPass<Tuple<IStatement, IMethod, ICompilerLog>, IStatement>
    {
        public AnalysisPass(Func<IMethod, ICompilerLog, INodeVisitor> CreateVisitor)
        {
            this.CreateVisitor = CreateVisitor;
        }

        public Func<IMethod, ICompilerLog, INodeVisitor> CreateVisitor { get; private set; }

        public IStatement Apply(Tuple<IStatement, IMethod, ICompilerLog> Value)
        {
            return CreateVisitor(Value.Item2, Value.Item3).Visit(Value.Item1);
        }
    }
}
