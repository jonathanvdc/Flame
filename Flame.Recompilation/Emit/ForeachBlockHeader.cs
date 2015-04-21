using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class ForeachBlockHeader : IForeachBlockHeader
    {
        public ForeachBlockHeader(RecompiledCodeGenerator CodeGenerator, IEnumerable<CollectionBlock> Collections)
        {
            var collElems = Collections.Select(item => new CollectionElement(item.Member, item.Collection)).ToArray();
            this.foreachStatement = new ForeachStatement(collElems);
        }

        public RecompiledCodeGenerator CodeGenerator { get; private set; }
        private ForeachStatement foreachStatement;
        private IReadOnlyList<IVariable> elems;

        public IReadOnlyList<IVariable> Elements
        {
            get 
            {
                if (elems == null)
                {
                    var foreachElems = new List<IVariable>();
                    foreach (var item in foreachStatement.Elements)
                    {
                        foreachElems.Add(new TypedEmitVariable(new RecompiledVariable(CodeGenerator, item), item.Type));
                    }
                    elems = foreachElems;
                }
                return elems; 
            }
        }

        public ForeachStatement ToForeachStatement(IStatement Body)
        {
            foreachStatement.Body = Body;
            return foreachStatement;
        }
    }
}
