using Flame;
using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.DSharp.Build;
using Flame.Syntax;
using Flame.Syntax.DSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PassTests.DSharp
{
    [TestClass]
    public class SemanticsTest
    {
        private static ISyntaxState CreateState()
        {
            var descMethod = new DescribedMethod("", null, PrimitiveTypes.Void, true);
            var emptyBinder = DSharpBuildHelpers.Instance.CreatePrimitiveBinder(Flame.Binding.EmptyBinder.Instance);
            var log = new TestLog(EmptyCompilerOptions.Instance);
            return new SyntaxState(descMethod, emptyBinder, log, new DSharpTypeNamer());
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
            return ConversionExpression.Create(CreateExpression(Code), GetType<T>());
        }

        public static bool EvaluatesTo<T>(string Code, T Value)
        {
            var expr = CreateExpression<T>(Code);
            return expr.EvaluatesTo<T>(Value);
        }

        [TestMethod]
        [TestCategory("D# - Semantics")]
        public void ParseLiteral()
        {
            var expr = CreateExpression<int>("1");
            Assert.IsTrue(expr.EvaluatesTo<int>(1));
        }

        [TestMethod]
        [TestCategory("D# - Semantics")]
        public void ParseSum()
        {
            Assert.IsTrue(EvaluatesTo<int>("1 + 2", 3));
        }
    }
}
