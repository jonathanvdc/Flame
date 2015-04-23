using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ILArgumentVariable : IUnmanagedEmitVariable
    {
        public ILArgumentVariable(ICodeGenerator CodeGenerator, int Index)
        {
            this.CodeGenerator = CodeGenerator;
            this.Index = Index;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public int Index { get; private set; }
        public IType Type { get { return ILCodeGenerator.GetExtendedParameterTypes(CodeGenerator)[Index]; } }

        public ICodeBlock EmitAddressOf()
        {
            return new ArgumentAddressOfBlock(CodeGenerator, Index);
        }

        public ICodeBlock EmitGet()
        {
            return new ArgumentGetBlock(CodeGenerator, Index);
        }

        public ICodeBlock EmitRelease()
        {
            return CodeGenerator.EmitVoid();
        }

        public ICodeBlock EmitSet(ICodeBlock Value)
        {
            return new ArgumentSetBlock(CodeGenerator, Index, (ICecilBlock)Value);
        }
    }
}
