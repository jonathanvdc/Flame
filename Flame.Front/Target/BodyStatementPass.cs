using Flame.Compiler;
using Flame.Compiler.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class BodyStatementPass : IPass<BodyPassArgument, IStatement>
    {
        public BodyStatementPass(IPass<IStatement, IStatement> StatementPass)
        {
            this.StatementPass = StatementPass;
        }

        public IPass<IStatement, IStatement> StatementPass { get; private set; }

        public IStatement Apply(BodyPassArgument Value)
        {
            return StatementPass.Apply(Value.Body);
        }

        public static PassInfo<BodyPassArgument, IStatement> ToBodyPass(PassInfo<IStatement, IStatement> StatementPass)
        {
            return new PassInfo<BodyPassArgument, IStatement>(new BodyStatementPass(StatementPass.Pass), StatementPass.Name, StatementPass.UsePass);
        }
    }
}
