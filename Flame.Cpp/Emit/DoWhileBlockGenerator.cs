using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class DoWhileBlockGenerator : CppBlockGeneratorBase
    {
        public DoWhileBlockGenerator(CppCodeGenerator CodeGenerator, ICppBlock Condition)
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

        public override ICppBlock Simplify()
        {
            return new DoWhileBlock(Condition, base.Simplify());
        }

        public override IEnumerable<CppLocal> LocalsUsed
        {
            get { return Condition.LocalsUsed.Union(base.LocalsUsed); }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
