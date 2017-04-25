using Flame;
using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Flame.DSharp.Build;
using Flame.Syntax;
using Flame.Syntax.DSharp;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.DSharp
{
    [TestFixture]
    public class SemanticsTests
    {
        private static ISyntaxState CreateState()
        {
            var descMethod = new DescribedMethod("", null, PrimitiveTypes.Void, true);
            var emptyBinder = DSharpBuildHelpers.Instance.CreatePrimitiveBinder(Flame.Binding.EmptyBinder.Instance);
            var log = new TestLog(EmptyCompilerOptions.Instance);
            return new SyntaxState(descMethod, emptyBinder, log, new DSharpTypeRenderer());
        }

        public static IStatement CreateStatement(string Code)
        {
            var syntax = ParserTests.ParseStatement(Code);
            return syntax.CreateVerifiedStatement(CreateState());
        }

        public static IExpression CreateExpression(string Code)
        {
            var syntax = ParserTests.ParseExpression(Code);
            return syntax.CreateVerifiedExpression(CreateState());
        }

        private static Dictionary<Type, IType> typeDict = new Dictionary<Type, IType>()
        {
            { typeof(sbyte), PrimitiveTypes.Int8  },
            { typeof(short), PrimitiveTypes.Int16 },
            { typeof(int),   PrimitiveTypes.Int32 },
            { typeof(long),  PrimitiveTypes.Int64 }
        };

        public static IType GetType<T>()
        {
            return typeDict[typeof(T)];
        }

        public static IExpression CreateExpression<T>(string Code)
        {
            return ConversionExpression.Instance.Create(CreateExpression(Code), GetType<T>());
        }

        public static bool EvaluatesTo<TE, TV>(string Code, TV Value)
        {
            var expr = CreateExpression<TE>(Code);
            return expr.EvaluatesTo<TV>(Value);
        }

        [Test]
        public void ParseLiteral()
        {
            Assert.IsTrue(EvaluatesTo<int, IntegerValue>("1", new IntegerValue(1)));
        }

        [Test]
        public void ParseSum()
        {
            Assert.IsTrue(EvaluatesTo<int, IntegerValue>("1 + 2", new IntegerValue(3)));
        }

        [Test]
        public void ParseExpressionLambda()
        {
            var expr = CreateExpression("int(int x) => x * 2");
            Assert.IsTrue(expr is LambdaExpression);
            var lambdaExpr = (LambdaExpression)expr;
            Assert.IsTrue(lambdaExpr.Header.CaptureList.Count == 0);
        }

        [Test]
        public void ParseStatementLambda()
        {
            var expr = CreateExpression("int(int x) => { return x * 2; }");
            Assert.IsTrue(expr is LambdaExpression);
            var lambdaExpr = (LambdaExpression)expr;
            Assert.IsTrue(lambdaExpr.Header.CaptureList.Count == 0);
        }

        [Test]
        public void ParseCapturingLambda()
        {
            CreateStatement("{ int x = 1; var f = int() => { return x * 2; }; }");
        }
    }
}
