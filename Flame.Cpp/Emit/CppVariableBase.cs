using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public abstract class CppVariableBase : IUnmanagedEmitVariable
    {
        public CppVariableBase(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public abstract ICppBlock CreateBlock();
        public abstract IType Type { get; }

        public virtual ICodeBlock EmitAddressOf()
        {
            return new AddressOfBlock(CreateBlock());
        }

        public virtual ICodeBlock EmitGet()
        {
            return CreateBlock();
        }

        public virtual ICodeBlock EmitRelease()
        {
            return new EmptyBlock(CodeGenerator);
        }

        public virtual ICodeBlock EmitSet(ICodeBlock Value)
        {
            return new VariableAssignmentBlock(CreateBlock(), (ICppBlock)Value);
        }

        public override string ToString()
        {
            return CreateBlock().GetCode().ToString();
        }
    }
}
