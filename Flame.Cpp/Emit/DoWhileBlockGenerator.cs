using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class DoWhileBlockGenerator : CppBlockGeneratorBase, ICppScopeBlock
    {
        public DoWhileBlockGenerator(ICodeGenerator CodeGenerator, ICppBlock Condition)
            : base(CodeGenerator)
        {
            this.Condition = Condition;
        }

        public ICppBlock Condition { get; private set; }

        public override IEnumerable<IHeaderDependency> Dependencies
        {
            get
            {
                return Condition.Dependencies.MergeDependencies(base.Dependencies);
            }
        }

        public override CodeBuilder GetCode()
        {
            var cb = new CodeBuilder();
            cb.AddLine("do");
            cb.AddBodyCodeBuilder(base.GetCode());
            cb.Append("while (");
            cb.Append(Condition.GetCode());
            cb.Append(");");
            return cb;
        }

        public override IEnumerable<CppLocal> LocalsUsed
        {
            get { return Condition.LocalsUsed.Concat(base.LocalsUsed).Distinct(); }
        }

        public bool DeclareVariable(CppLocal Local)
        {
            if (Condition.UsesLocal(Local))
            {
                return false;
            }
            else
            {
                var singleBlock = GetSingleLocalUsingBlock(Local) as ICppScopeBlock;
                if (singleBlock != null && singleBlock.DeclareVariable(Local))
                {
                    return true;
                }
                DeclareCore(Local);
                return true;
            }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
