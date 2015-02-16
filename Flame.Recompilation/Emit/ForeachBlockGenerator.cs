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
    public class ForeachBlockGenerator : RecompiledBlockGenerator, IForeachBlockGenerator
    {
        public ForeachBlockGenerator(RecompiledCodeGenerator CodeGenerator, IEnumerable<CollectionBlock> Collections)
            : base(CodeGenerator)
        {
            var collElems = Collections.Select((item) => new CollectionElement(item.Member, item.Collection)).ToArray();
            this.foreachStatement = new ForeachStatement(collElems);
        }

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
                        foreachElems.Add(new RecompiledVariable(CodeGenerator, item));
                    }
                    elems = foreachElems;
                }
                return elems; 
            }
        }

        public override IStatement GetStatement()
        {
            this.foreachStatement.Body = base.GetStatement();
            return this.foreachStatement;
        }
    }
}
