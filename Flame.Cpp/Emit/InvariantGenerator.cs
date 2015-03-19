using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class InvariantGenerator : IInvariantGenerator
    {
        public InvariantGenerator(TypeInvariants Invariants)
        {
            this.Invariants = Invariants;
        }

        public TypeInvariants Invariants { get; private set; }

        public ICodeGenerator CodeGenerator
        {
            get { return Invariants.CodeGenerator; }
        }

        public void EmitInvariant(ICodeBlock Block)
        {
            Invariants.AddInvariant((ICppBlock)Block);
        }
    }
}
