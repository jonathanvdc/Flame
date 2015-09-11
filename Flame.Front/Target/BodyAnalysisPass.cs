using Flame.Compiler;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class BodyAnalysisPass : IPass<BodyPassArgument, IStatement>
    {
        public BodyAnalysisPass(IPass<Tuple<IStatement, IMethod, ICompilerLog>, IStatement> AnalysisPass)
        {
            this.AnalysisPass = AnalysisPass;
        }

        public IPass<Tuple<IStatement, IMethod, ICompilerLog>, IStatement> AnalysisPass { get; private set; }

        public IStatement Apply(BodyPassArgument Value)
        {
            return AnalysisPass.Apply(Tuple.Create(Value.Body, Value.DeclaringMethod, Value.PassEnvironment.Log));
        }
    }
}
