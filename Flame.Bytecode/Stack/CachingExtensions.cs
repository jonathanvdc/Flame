using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Flame.Compiler.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode.Stack
{
    public static class CachingExtensions
    {
        public static CachedExpression CacheTo(this IExpression Expression, IVariable Variable)
        {
            return new CachedExpression()
            {
                InitializedCacheStatement = Variable.CreateSetStatement(Expression),
                RetrieveExpression = new InitializedExpression(
                    EmptyStatement.Instance,
                    Variable.CreateGetExpression(),
                    Variable.CreateReleaseStatement())
            };
        }
        public static CachedExpression Cache(this IExpression Expression)
        {
            var local = new LocalVariable(Expression.Type);
            return Expression.CacheTo(local);
        }
    }
}
