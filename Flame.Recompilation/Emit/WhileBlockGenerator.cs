using Flame.Compiler;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class WhileBlockGenerator : RecompiledBlockGenerator
    {
        public WhileBlockGenerator(RecompiledCodeGenerator CodeGenerator, IExpression Condition)
            : base(CodeGenerator)
        {
            this.Condition = Condition;
        }

        public IExpression Condition { get; private set; }

        public override IStatement GetStatement()
        {
            var body = base.GetStatement();
            return new WhileStatement(Condition, body);
        }
    }
    public class DoWhileBlockGenerator : RecompiledBlockGenerator
    {
        public DoWhileBlockGenerator(RecompiledCodeGenerator CodeGenerator, IExpression Condition)
            : base(CodeGenerator)
        {
            this.Condition = Condition;
        }

        public IExpression Condition { get; private set; }

        public override IStatement GetStatement()
        {
            var body = base.GetStatement();
            return new DoWhileStatement(body, Condition);
        }
    }
    public class ForBlockGenerator : RecompiledBlockGenerator
    {
        public ForBlockGenerator(RecompiledCodeGenerator CodeGenerator, IStatement Initialization, IExpression Condition, IStatement Delta)
            : base(CodeGenerator)
        {
            this.Initialization = Initialization;
            this.Condition = Condition;
            this.Delta = Delta;
        }

        public IStatement Initialization { get; private set; }
        public IExpression Condition { get; private set; }
        public IStatement Delta { get; private set; }

        public override IStatement GetStatement()
        {
            var body = base.GetStatement();
            return new ForStatement(Initialization, Condition, Delta, body);
        }
    }
}
