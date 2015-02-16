using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class UnaryOperation : IPythonBlock
    {
        public UnaryOperation(ICodeGenerator CodeGenerator, IPythonBlock Value, Operator Operator)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
            this.Operator = Operator;
        }

        public IPythonBlock Value { get; private set; }
        public Operator Operator { get; private set; }

        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            string opString = GetOperatorString(Operator);
            cb.Append(opString);
            if (Operator.Equals(Operator.Not))
            {
                cb.Append(" ");
            }
            if (Operator.Equals(Operator.Hash) || (Value is BinaryOperation && !Operator.Equals(Operator.Not)))
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
                return "not";
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

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Value.GetDependencies();
        }
    }
}
