using Flame.Compiler;
using Flame.DSharp.Lexer;
using Flame.DSharp.Parser;
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
    public class ParserTests
    {
        public static IStatementSyntax ParseStatement(string Code)
        {
            var lexer = new TokenizerStream(new SourceDocument(Code, "test"));
            var log = new TestLog(EmptyCompilerOptions.Instance);
            var parser = new DSharpSyntaxParser(log);
            return parser.ParseStatement(lexer);
        }

        public static IExpressionSyntax ParseExpression(string Code)
        {
            var lexer = new TokenizerStream(new SourceDocument(Code, "test"));
            var log = new TestLog(EmptyCompilerOptions.Instance);
            var parser = new DSharpSyntaxParser(log);
            return parser.ParseExpression(lexer);
        }

        public static ITypeSyntax ParseType(string Code)
        {
            var lexer = new TokenizerStream(new SourceDocument(Code, "test"));
            var log = new TestLog(EmptyCompilerOptions.Instance);
            var parser = new DSharpSyntaxParser(log);
            return parser.ParseType(lexer);
        }

        [Test]
        public void PeekDelegate()
        {
            var lexer = new TokenizerStream(new SourceDocument("x(y, z) i ", "test"));
            var log = new TestLog(EmptyCompilerOptions.Instance);
            var parser = new DSharpSyntaxParser(log);
            var pos = lexer.CurrentPosition;
            var result = parser.PeekEntireType(lexer, true, ref pos);
            Assert.IsTrue(result is DelegateTypeSyntax);
            var peek = lexer.PeekNoTrivia(pos);
            Assert.IsTrue(peek.Type == TokenType.Identifier);
            lexer.Seek(pos);
            var next = lexer.NextNoTrivia();
            Assert.IsTrue(next.Type == TokenType.Identifier);
        }

        [Test]
        public void ParseDelegate()
        {
            var type = ParseType("x(y, z<w>)");
            Assert.IsTrue(type is DelegateTypeSyntax);
        }

        [Test]
        public void ParseDelegateDeclExpr()
        {
            var expr = ParseExpression("x(y u, z<w> v) i ");
            Assert.IsTrue(expr is InlineVariableDeclarationSyntax);
        }

        [Test]
        public void ParseInvocationExpr()
        {
            var expr = ParseExpression("x(y, z)");
            Assert.IsTrue(expr is InvocationSyntax);
        }

        [Test]
        public void ParseLambda()
        {
            var expr = ParseExpression("bool(int x, int y) => x == y");
            Assert.IsTrue(expr is LambdaSyntax);
        }
    }
}
