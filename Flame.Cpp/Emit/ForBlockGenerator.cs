using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ForBlockGenerator : CppBlockGeneratorBase
    {
        public ForBlockGenerator(CppCodeGenerator CodeGenerator, ICppBlock Initialization, ICppBlock Condition, ICppBlock Delta)
            : base(CodeGenerator)
        {
            this.Initialization = Initialization;
            this.Condition = Condition;
            this.Delta = Delta;
            foreach (var item in Delta.GetLocalDeclarations())
            {
                if (Initialization.DeclaresLocal(item.Local))
                {
                    item.DeclareVariable = false;
                }
            }
        }

        public ICppBlock Initialization { get; private set; }
        public ICppBlock Condition { get; private set; }
        public ICppBlock Delta { get; private set; }

        public override ICppBlock Simplify()
        {
            return new ForBlock(Initialization, Condition, Delta, base.Simplify());
        }
    }
}
