using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public abstract class CppVariableBase : IUnmanagedVariable
    {
        public CppVariableBase(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public abstract ICppBlock CreateBlock();
        public abstract IType Type { get; }

        public virtual IExpression CreateGetExpression()
        {
            return new CodeBlockExpression(CreateBlock(), Type);
        }

        public virtual IStatement CreateReleaseStatement()
        {
            return new EmptyStatement();
        }

        public virtual IStatement CreateSetStatement(IExpression Value)
        {
            return new ExpressionStatement(new CodeBlockExpression(new VariableAssignmentBlock(CreateBlock(), (ICppBlock)Value.Emit(CodeGenerator)), Type));
        }

        public override string ToString()
        {
            return CreateBlock().GetCode().ToString();
        }

        public virtual IExpression CreateAddressOfExpression()
        {
            return new CodeBlockExpression(new AddressOfBlock(CreateBlock()), Type.MakePointerType(PointerKind.TransientPointer));
        }
    }
}
