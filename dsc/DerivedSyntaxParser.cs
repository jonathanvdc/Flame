//#define TEST_PARSER
#if TEST_PARSER

using Flame.DSharp.Lexer;
using Flame.DSharp.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public class DerivedSyntaxParser : DSharpSyntaxParser
    {
        public DerivedSyntaxParser()
            : base(ConsoleLog.Instance)
        {

        }

        public static void Test()
        {
            var codeString = "/* /// This is a strange comment.\r\n /// Even more comments.\r\n public void X() { return; } */ public class Foo { }";
            var tokenizer = new TokenizerStream(codeString);
            Token item;
            while ((item = tokenizer.Next()).Type != TokenType.EndOfFile)
            {
                Console.WriteLine(item);
            }
        }
    }
}

#endif
