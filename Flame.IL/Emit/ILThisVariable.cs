using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class ILThisVariable : IUnmanagedVariable
    {
        public ILThisVariable(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public IType Type
        {
            get { return CodeGenerator.Method.DeclaringType; }
        }
        
        public IExpression CreateGetExpression()
        {
            return new CodeBlockExpression(new ArgumentGetInstruction(CodeGenerator, 0), Type);
        }

        public IStatement CreateReleaseStatement()
        {
            return new EmptyStatement();
        }

        public IStatement CreateSetStatement(IExpression Value)
        {
            return new CodeBlockStatement(new ArgumentSetInstruction(CodeGenerator, 0, Value.Emit(CodeGenerator)));
        }

        public IExpression CreateAddressOfExpression()
        {
            return new CodeBlockExpression(new ArgumentAddressOfInstruction(CodeGenerator, 0), Type);
        }
    }
}
