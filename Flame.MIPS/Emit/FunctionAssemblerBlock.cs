using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class FunctionAssemblerBlock : IAssemblerBlock
    {
        public FunctionAssemblerBlock(ICodeGenerator CodeGenerator, IType Type, Func<IAssemblerEmitContext, IEnumerable<IStorageLocation>> Function)
        {
            this.CodeGenerator = CodeGenerator;
            this.Type = Type;
            this.func = Function;
        }

        private Func<IAssemblerEmitContext, IEnumerable<IStorageLocation>> func;
        public IType Type { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            return func(Context);
        }
    }

    public class ActionAssemblerBlock : IAssemblerBlock
    {
        public ActionAssemblerBlock(ICodeGenerator CodeGenerator, Action<IAssemblerEmitContext> Action)
        {
            this.CodeGenerator = CodeGenerator;
            this.func = Action;
        }

        private Action<IAssemblerEmitContext> func;
        public IType Type { get { return PrimitiveTypes.Void; } }
        public ICodeGenerator CodeGenerator { get; private set; }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            func(Context);
            return new IStorageLocation[0];
        }
    }
}
