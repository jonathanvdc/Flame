using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class AssemblerArgument : IEmitVariable
    {
        public AssemblerArgument(ICodeGenerator CodeGenerator, int Index)
        {
            this.CodeGenerator = CodeGenerator;
            this.Index = Index;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public int Index { get; private set; }

        public ICodeBlock EmitGet()
        {
            var t = Type;
            return new FunctionAssemblerBlock(CodeGenerator, t, (context) =>
            {
                var arg = context.GetArgument(Index);
                return new IStorageLocation[] { arg };
            });
        }

        public ICodeBlock EmitRelease()
        {
            return CodeGenerator.EmitVoid();
        }

        public ICodeBlock EmitSet(ICodeBlock Value)
        {
            return new ActionAssemblerBlock(CodeGenerator, (context) =>
            {
                var arg = context.GetArgument(Index);
                ((IAssemblerBlock)Value).EmitStoreTo(arg, context);
            });
        }

        public IType Type
        {
            get { return CodeGenerator.Method.GetParameters()[Index].ParameterType; }
        }
    }
}
