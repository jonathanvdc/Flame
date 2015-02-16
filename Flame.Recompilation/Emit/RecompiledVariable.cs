using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class RecompiledVariable : IUnmanagedVariable
    {
        public RecompiledVariable(ICodeGenerator CodeGenerator, IVariable Variable)
        {
            this.CodeGenerator = CodeGenerator;
            this.Variable = Variable;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IVariable Variable { get; private set; }

        public IExpression CreateGetExpression()
        {
            var expr = Variable.CreateGetExpression();
            return new CodeBlockExpression(new ExpressionBlock(CodeGenerator, expr), expr.Type);
        }

        public IStatement CreateReleaseStatement()
        {
            return new CodeBlockStatement(new StatementBlock(CodeGenerator, Variable.CreateReleaseStatement()));
        }

        public IStatement CreateSetStatement(IExpression Value)
        {
            var val = Value.Emit(CodeGenerator);
            var valExpr = RecompiledCodeGenerator.GetExpression(val);
            return new CodeBlockStatement(new StatementBlock(CodeGenerator, Variable.CreateSetStatement(valExpr)));
        }

        public IExpression CreateAddressOfExpression()
        {
            if (Variable is IUnmanagedVariable)
            {
                var expr = ((IUnmanagedVariable)Variable).CreateAddressOfExpression();
                return new CodeBlockExpression(new ExpressionBlock(CodeGenerator, expr), expr.Type);
            }
            else
            {
                throw new InvalidOperationException("Tried to take the address of a fully managed variable.");
            }
        }

        public IType Type
        {
            get { return Variable.Type; }
        }
    }
}
