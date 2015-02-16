using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class CollectionBlock : ICollectionBlock
    {
        public CollectionBlock(ICodeGenerator CodeGenerator, IVariableMember Member, IExpression Collection)
        {
            this.CodeGenerator = CodeGenerator;
            this.Member = Member;
            this.Collection = Collection;
        }

        public IVariableMember Member { get; private set; }
        public IExpression Collection { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }
    }
}
