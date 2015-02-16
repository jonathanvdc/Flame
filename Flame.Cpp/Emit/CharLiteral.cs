using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CharLiteral : ICppBlock
    {
        public CharLiteral(ICodeGenerator CodeGenerator, char Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }

        public char Value { get; private set; }
        public IType Type { get { return PrimitiveTypes.Char; } }
        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append(CharLiteral.ToLiteral(Value));
            return cb;
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return new IHeaderDependency[0]; }
        }

        // Slightly adapted version of Smilediver's answer of stackoverflow question http://stackoverflow.com/questions/323640/can-i-convert-a-c-sharp-string-value-to-an-escaped-string-literal
        public static void AppendLiteralChar(StringBuilder literal, char c)
        {
            switch (c)
            {
                case '\'': literal.Append(@"\'"); break;
                case '\"': literal.Append("\\\""); break;
                case '\\': literal.Append(@"\\"); break;
                case '\0': literal.Append(@"\0"); break;
                case '\a': literal.Append(@"\a"); break;
                case '\b': literal.Append(@"\b"); break;
                case '\f': literal.Append(@"\f"); break;
                case '\n': literal.Append(@"\n"); break;
                case '\r': literal.Append(@"\r"); break;
                case '\t': literal.Append(@"\t"); break;
                case '\v': literal.Append(@"\v"); break;
                default:
                    // ASCII printable character
                    if (c >= 0x20 && c <= 0x7e)
                    {
                        literal.Append(c);
                        // As UTF16 escaped character
                    }
                    else
                    {
                        literal.Append(@"\u");
                        literal.Append(((int)c).ToString("x4"));
                    }
                    break;
            }
        }

        // Slightly adapted version of Smilediver's answer of stackoverflow question http://stackoverflow.com/questions/323640/can-i-convert-a-c-sharp-string-value-to-an-escaped-string-literal
        public static string ToLiteral(char Value)
        {
            StringBuilder literal = new StringBuilder(3);
            literal.Append('\'');
            AppendLiteralChar(literal, Value);
            literal.Append('\'');
            return literal.ToString();
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return new CppLocal[0]; }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
