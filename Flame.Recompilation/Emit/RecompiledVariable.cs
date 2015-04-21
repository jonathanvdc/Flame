using Flame.Compiler;
using Flame.Compiler.Emit;
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
    public class RecompiledVariable : IUnmanagedEmitVariable
    {
        public RecompiledVariable(ICodeGenerator CodeGenerator, IVariable Variable)
        {
            this.CodeGenerator = CodeGenerator;
            this.Variable = Variable;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IVariable Variable { get; private set; }

        public ICodeBlock EmitGet()
        {
            var expr = Variable.CreateGetExpression();
            return new ExpressionBlock(CodeGenerator, expr);
        }

        public ICodeBlock EmitAddressOf()
        {
            if (Variable is IUnmanagedVariable)
            {
                var expr = ((IUnmanagedVariable)Variable).CreateAddressOfExpression();
                return new ExpressionBlock(CodeGenerator, expr);
            }
            else
            {
                throw new InvalidOperationException("Tried to take the address of a fully managed variable.");
            }
        }

        public ICodeBlock EmitSet(ICodeBlock Value)
        {
            var valExpr = RecompiledCodeGenerator.GetExpression(Value);
            return new StatementBlock(CodeGenerator, Variable.CreateSetStatement(valExpr));
        }

        public ICodeBlock EmitRelease()
        {
            return new StatementBlock(CodeGenerator, Variable.CreateReleaseStatement());
        }
    }
}
