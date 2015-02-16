using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class UnaryOperation : ICppBlock
    {
        public UnaryOperation(ICodeGenerator CodeGenerator, ICppBlock Value, Operator Operator)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
            this.Operator = Operator;
        }

        public ICppBlock Value { get; private set; }
        public Operator Operator { get; private set; }

        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            string opString = GetOperatorString(Operator);
            cb.Append(opString);
            if (opString.Length > 1)
            {
                cb.Append(" ");
            }
            if (Value is BinaryOperation)
            {
                cb.Append(BinaryOperation.GetEnclosedCode(Value));
            }
            else
            {
                cb.Append(Value.GetCode());
            }
            return cb;
        }

        public static string GetOperatorString(Operator Operator)
        {
            if (Operator.Equals(Operator.Not))
            {
                return "!";
            }
            else
            {
                return Operator.Name;
            }
        }

        public IType Type
        {
            get { return Value.Type; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Value.LocalsUsed; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Value.Dependencies; }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
