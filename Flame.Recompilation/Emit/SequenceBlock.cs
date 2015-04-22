using Flame.Compiler;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class SequenceBlock : IStatementBlock
    {
        public SequenceBlock(ICodeGenerator CodeGenerator, ICodeBlock First, ICodeBlock Second)
        {
            this.CodeGenerator = CodeGenerator;
            this.First = First;
            this.Second = Second;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICodeBlock First { get; private set; }
        public ICodeBlock Second { get; private set; }

        public IEnumerable<IType> ResultTypes
        {
            get { return RecompiledCodeGenerator.GetResultTypes(First).Concat(RecompiledCodeGenerator.GetResultTypes(Second)); }
        }

        public IStatement GetStatement()
        {
            return new BlockStatement(new IStatement[] { RecompiledCodeGenerator.GetStatement(First), RecompiledCodeGenerator.GetStatement(Second) });
        }        
    }
}
