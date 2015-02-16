using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ILElementVariable : IUnmanagedVariable
    {
        public ILElementVariable(ICodeGenerator CodeGenerator, ICecilBlock Container, ICecilBlock[] Arguments)
        {
            this.CodeGenerator = CodeGenerator;
            this.Container = Container;
            this.Arguments = Arguments;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICecilBlock Container { get; private set; }
        public ICecilBlock[] Arguments { get; private set; }

        public IType Type
        {
            get 
            {
                var typeStack = new TypeStack();
                Container.StackBehavior.Apply(typeStack);
                return typeStack.Pop();
            }
        }
        
        public IExpression CreateAddressOfExpression()
        {
            return new CodeBlockExpression(new ElementAddressOfBlock(this), Type);
        }

        public IExpression CreateGetExpression()
        {
            return new CodeBlockExpression(new ElementGetBlock(this), Type);
        }

        public IStatement CreateReleaseStatement()
        {
            return new EmptyStatement();
        }

        public IStatement CreateSetStatement(IExpression Value)
        {
            return new CodeBlockStatement(new ElementSetBlock(this, (ICecilBlock)Value.Emit(CodeGenerator)));
        }
    }
}
