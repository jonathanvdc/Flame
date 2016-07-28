using Flame.Compiler;
using Flame.DSharp.Lexer;
using Loyc.MiniTest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.DSharp
{
    [TestFixture]
    public class LexerTests
    {
        private IEnumerable<Token> Lex(string Code)
        {
            var lexer = new TokenizerStream(new SourceDocument(Code, "test"));
            do
            {
                var token = lexer.Next();
                if (token.Type == TokenType.EndOfFile)
                {
                    break;
                }
                else
                {
                    yield return token;
                }
            } while (true);
        }

        private void AssertLex(string Code, params TokenType[] Types)
        {
            foreach (var item in Lex(Code).Zip(Types, Tuple.Create))
            {
                Assert.IsTrue(item.Item1.Type == item.Item2);
            }
        }

        [Test]
        public void LexIdentifier()
        {
            AssertLex("x", TokenType.Identifier);
        }

        [Test]
        public void LexIdentifierWithWhitespace()
        {
            AssertLex(" x ", TokenType.Whitespace, TokenType.Identifier, TokenType.Whitespace);
        }

        [Test]
        public void LexInteger()
        {
            AssertLex("10", TokenType.Integer);
        }

        [Test]
        public void LexKeywords()
        {
            AssertLex("this base class struct", TokenType.ThisKeyword, TokenType.Whitespace, TokenType.BaseKeyword, TokenType.Whitespace, TokenType.ClassKeyword, TokenType.Whitespace, TokenType.StructKeyword);
        }
    }
}
