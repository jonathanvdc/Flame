using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class AnalyzedTokenVariable : AnalyzedVariableBase
    {
        public AnalyzedTokenVariable(ICodeGenerator CodeGenerator, IVariable Variable, AnalysisToken Token)
            : base(CodeGenerator)
        {
            this.tokenVar = Variable;
            this.Token = Token;
        }

        private readonly IVariable tokenVar;
        public override IVariable GetVariable(VariableMetrics Metrics)
        {
            return tokenVar;
        }

        public override bool IsLocal
        {
            get { return true; }
        }

        public override IType Type
        {
            get { return tokenVar.Type; }
        }

        public AnalysisToken Token { get; private set; }

        public override int GetHashCode()
        {
            return Token.GetHashCode();
        }

        public override bool Equals(IAnalyzedVariable other)
        {
            if (other is AnalyzedTokenVariable)
            {
                return this.Token == ((AnalyzedTokenVariable)other).Token;
            }
            else
            {
                return false;
            }
        }
    }

    public class AnalyzedVariableGetBlock : AnalyzedExpressionBase<AnalyzedVariableGetBlock>
    {
        public AnalyzedVariableGetBlock(AnalyzedVariableBase Local)
            : base(Local.CodeGenerator)
        {
            this.Local = Local;
        }

        public AnalyzedVariableBase Local { get; private set; }

        public override IExpression ToExpression(VariableMetrics Metrics)
        {
            return Local.InliningCache.CreateGetExpression(Metrics);
        }

        public override VariableMetrics Metrics
        {
            get { return VariableMetrics.CreateFromReturns(Local); }
        }

        public override IExpressionProperties ExpressionProperties
        {
            get { return new VariableBlockProperties(Local.Type, Local.IsLocal); }
        }

        public override bool Equals(AnalyzedVariableGetBlock Other)
        {
            return this.Local.Equals(Other.Local);
        }

        public override int GetHashCode()
        {
            return this.Local.GetHashCode();
        }
    }

    public class AnalyzedVariableAddressOfBlock : AnalyzedExpressionBase<AnalyzedVariableAddressOfBlock>
    {
        public AnalyzedVariableAddressOfBlock(AnalyzedVariableBase Local)
            : base(Local.CodeGenerator)
        {
            this.Local = Local;
        }

        public AnalyzedVariableBase Local { get; private set; }

        public override IExpression ToExpression(VariableMetrics Metrics)
        {
            return Local.InliningCache.CreateAddressOfExpression();
        }

        public override VariableMetrics Metrics
        {
            get { return VariableMetrics.CreateFromReturns(Local); }
        }

        public override IExpressionProperties ExpressionProperties
        {
            get { return new VariableBlockProperties(Local.Type, Local.IsLocal); }
        }

        public override bool Equals(AnalyzedVariableAddressOfBlock Other)
        {
            return this.Local.Equals(Other.Local);
        }

        public override int GetHashCode()
        {
            return this.Local.GetHashCode();
        }
    }

    public class AnalyzedVariableSetBlock : AnalyzedStatementBase<AnalyzedVariableSetBlock>
    {
        public AnalyzedVariableSetBlock(AnalyzedVariableBase Local, IAnalyzedExpression Value)
            : base(Local.CodeGenerator)
        {
            this.Local = Local;
            this.Value = Value;
        }

        public AnalyzedVariableBase Local { get; private set; }
        public IAnalyzedExpression Value { get; private set; }

        public override IStatement ToStatement(VariableMetrics Metrics)
        {
            return Local.InliningCache.CreateSetStatement(Value, Metrics);
        }

        public override VariableMetrics Metrics
        {
            get { return Value.Metrics.PipeStore(Local); }
        }

        public override IStatementProperties StatementProperties
        {
            get { return new VariableBlockProperties(PrimitiveTypes.Void, true); }
        }

        public override bool Equals(AnalyzedVariableSetBlock Other)
        {
            return this.Local.Equals(Other.Local);
        }

        public override int GetHashCode()
        {
            return this.Local.GetHashCode();
        }
    }

    public class AnalyzedVariableReleaseStatement : AnalyzedStatementBase<AnalyzedVariableReleaseStatement>
    {
        public AnalyzedVariableReleaseStatement(AnalyzedVariableBase Local)
            : base(Local.CodeGenerator)
        {
            this.Local = Local;
        }

        public AnalyzedVariableBase Local { get; private set; }

        public override IStatement ToStatement(VariableMetrics Metrics)
        {
            Local.InliningCache.Release(Metrics);
            return Local.GetVariable(Metrics).CreateReleaseStatement();
        }

        public override VariableMetrics Metrics
        {
            get { return new VariableMetrics(); }
        }

        public override IStatementProperties StatementProperties
        {
            get { return new VariableBlockProperties(PrimitiveTypes.Void, Local.IsLocal); }
        }

        public override bool Equals(AnalyzedVariableReleaseStatement Other)
        {
            return this.Local.Equals(Other.Local);
        }

        public override int GetHashCode()
        {
            return this.Local.GetHashCode();
        }
    }
}
