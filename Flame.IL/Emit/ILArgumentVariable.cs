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
    public class ILArgumentVariable : IUnmanagedVariable
    {
        public ILArgumentVariable(ICodeGenerator CodeGenerator, int Index)
        {
            this.CodeGenerator = CodeGenerator;
            this.Index = Index;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public int Index { get; private set; }

        public IType Type
        {
            get { return CodeGenerator.Method.GetParameters()[Index].ParameterType; }
        }

        public IExpression CreateGetExpression()
        {
            return new CodeBlockExpression(new ArgumentGetInstruction(CodeGenerator, Index), Type);
        }

        public IStatement CreateReleaseStatement()
        {
            return new EmptyStatement();
        }

        public IStatement CreateSetStatement(IExpression Value)
        {
            return new CodeBlockStatement(new ArgumentSetInstruction(CodeGenerator, Index, Value.Emit(CodeGenerator)));
        }

        public IExpression CreateAddressOfExpression()
        {
            return new CodeBlockExpression(new ArgumentAddressOfInstruction(CodeGenerator, Index), Type);
        }
    }
}
