using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Compiler.Emit
{
    public abstract class BranchingCodeGenerator : CodeGeneratorBase, IBranchingCodeGenerator
    {
        public abstract ILabel CreateLabel();

        public override IIfElseBlockGenerator CreateIfElseBlock(ICodeBlock Condition)
        {
            return new BranchingIfElseBlock(this, Condition); 
        }
    }
}
