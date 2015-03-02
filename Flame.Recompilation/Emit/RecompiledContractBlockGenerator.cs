using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class RecompiledContractBlockGenerator : RecompiledBlockGenerator, IContractBlockGenerator
    {
        public RecompiledContractBlockGenerator(RecompiledCodeGenerator CodeGenerator)
            : base(CodeGenerator)
        {
            this.Preconditions = new List<IExpression>();
            this.Postconditions = new List<IExpression>();
        }

        public IList<IExpression> Preconditions { get; private set; }
        public IList<IExpression> Postconditions { get; private set; }

        public void EmitPostcondition(ICodeBlock Block)
        {
            Postconditions.Add(RecompiledCodeGenerator.GetExpression(Block));
        }

        public void EmitPrecondition(ICodeBlock Block)
        {
            Preconditions.Add(RecompiledCodeGenerator.GetExpression(Block));
        }

        public override IStatement GetStatement()
        {
            var body = base.GetStatement();
            if (Preconditions.Count == 0 && Postconditions.Count == 0)
            {
                return body;
            }
            else
            {
                return new ContractBodyStatement(body, Preconditions, Postconditions);
            }
        }
    }
}
