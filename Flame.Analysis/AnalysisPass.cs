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
    public class AnalysisPass : IPass<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>
    {
        public AnalysisPass(INodeVisitor Visitor)
        {
            this.Visitor = Visitor;
        }

        public INodeVisitor Visitor { get; private set; }

        public Tuple<IStatement, IMethod> Apply(Tuple<IStatement, IMethod> Value)
        {
            Visitor.Visit(Value.Item1);
            return Value;
        }
    }
}
