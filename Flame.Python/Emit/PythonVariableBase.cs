using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public abstract class PythonVariableBase : IEmitVariable
    {
        public PythonVariableBase(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public abstract IPythonBlock CreateGetBlock();
        public abstract IType Type { get; }

        public ICodeBlock EmitGet()
        {
            return CreateGetBlock();
        }

        public virtual ICodeBlock EmitRelease()
        {
            return new EmptyBlock(CodeGenerator);
        }

        public ICodeBlock EmitSet(ICodeBlock Value)
        {
            return new AssignmentBlock(CodeGenerator, CreateGetBlock(), (IPythonBlock)Value);
        }

        public CodeBuilder GetCode()
        {
            return CreateGetBlock().GetCode();
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
