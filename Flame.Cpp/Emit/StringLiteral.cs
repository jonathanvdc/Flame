using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class StringLiteral : ICppBlock
    {
        public StringLiteral(ICodeGenerator CodeGenerator, string Value)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
        }

        public string Value { get; private set; }
        public IType Type { get { return PrimitiveTypes.String; } }
        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append(ToLiteral(Value, '"'));
            return cb;
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return new IHeaderDependency[0]; }
        }

        // Slightly adapted version of Smilediver's answer of stackoverflow question http://stackoverflow.com/questions/323640/can-i-convert-a-c-sharp-string-value-to-an-escaped-string-literal
        public static string ToLiteral(string Input, char Delimiter)
        {
            StringBuilder literal = new StringBuilder(Input.Length + 2);
            literal.Append(Delimiter);
            foreach (var c in Input)
            {
                CharLiteral.AppendLiteralChar(literal, c);
            }
            literal.Append(Delimiter);
            return literal.ToString();
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return new CppLocal[0]; }
        }
    }
}
