using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public interface IAnalyzedBlock : ICodeBlock, IEquatable<IAnalyzedBlock>
    {
        /// <summary>
        /// Gets the block's variable metrics.
        /// </summary>
        VariableMetrics Metrics { get; }

        /// <summary>
        /// Gets the block's properties.
        /// </summary>
        IBlockProperties Properties { get; }

        /// <summary>
        /// Gets the block's initialization statement.
        /// </summary>
        /// <returns></returns>
        IAnalyzedStatement InitializationStatement { get; }
    }

    public interface IBlockProperties
    {
        /// <summary>
        /// Gets a boolean value that indicates whether the given block is volatile, i.e., can cause an arbitrary state change.
        /// </summary>
        bool IsVolatile { get; }
    }

    public interface IExpressionProperties : IBlockProperties
    {
        /// <summary>
        /// Gets a boolean value that indicates whether this expression should occur inline, even if it is constant.
        /// </summary>
        bool Inline { get; }

        /// <summary>
        /// Gets the expression's type.
        /// </summary>
        IType Type { get; }
    }

    public interface IStatementProperties : IBlockProperties
    {

    }

    public interface IAnalyzedStatement : IAnalyzedBlock
    {
        /// <summary>
        /// Gets the statement block's properties.
        /// </summary>
        IStatementProperties StatementProperties { get; }

        /// <summary>
        /// Gets a portable statement that represents the analyzed statement.
        /// </summary>
        /// <param name="Metrics"></param>
        /// <returns></returns>
        IStatement ToStatement(VariableMetrics State);
    }

    public interface IAnalyzedExpression : IAnalyzedBlock
    {
        /// <summary>
        /// Gets the expression block's properties.
        /// </summary>
        IExpressionProperties ExpressionProperties { get; }

        /// <summary>
        /// Gets a portable expression that represents the analyzed expression.
        /// </summary>
        /// <param name="Metrics"></param>
        /// <returns></returns>
        IExpression ToExpression(VariableMetrics State);
    }
}
