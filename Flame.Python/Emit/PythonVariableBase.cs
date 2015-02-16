using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public abstract class PythonVariableBase : IVariable
    {
        public PythonVariableBase(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public abstract IPythonBlock CreateGetBlock();
        public abstract IType Type { get; }

        public IExpression CreateGetExpression()
        {
            return new CodeBlockExpression(CreateGetBlock(), Type);
        }

        public virtual IStatement CreateReleaseStatement()
        {
            return new EmptyStatement();
        }

        public IStatement CreateSetStatement(IExpression Value)
        {
            return new CodeBlockStatement(new AssignmentBlock(CodeGenerator, CreateGetBlock(), (IPythonBlock)Value.Emit(CodeGenerator)));
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
