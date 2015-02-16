using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public static class OptimizationExtensions
    {
        #region Method Analysis

        public static bool CanAnalyzeMethod(this IMethod Method)
        {
            return Method is IBodyMethod;
        }

        public static IAnalyzedStatement AnalyzeMethodBody(this IBodyMethod Method)
        {
            var stmt = Method.GetMethodBody();
            var cg = new AnalyzingCodeGenerator(Method);
            var block = cg.CreateBlock();
            stmt.Emit(block);
            return (IAnalyzedStatement)block;
        }

        #endregion

        public static IStatement GetOptimizedBody(this IBodyMethod Method)
        {
            var analyzedStmt = Method.AnalyzeMethodBody();
            var stmt = analyzedStmt.ToStatement(new VariableMetrics());
            return stmt.Optimize();
        }
    }
}
