using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class VariableAssignmentBlock : ICppBlock
    {
        public VariableAssignmentBlock(ICppBlock Target, ICppBlock Value)
        {
            this.Target = Target;
            this.Value = Value;
        }

        public ICppBlock Target { get; private set; }
        public ICppBlock Value { get; private set; }

        public IType Type
        {
            get { return Target.Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Target.Dependencies.MergeDependencies(Value.Dependencies); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Target.LocalsUsed.Concat(Value.LocalsUsed).Distinct(); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Target.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            var targetCode = Target.GetCode();
            if (Value is BinaryOperation)
            {
                var binOp = (BinaryOperation)Value;
                if (BinaryOperation.IsAssignableBinaryOperator(binOp.Operator) && binOp.Left.GetCode().ToString() == targetCode.ToString())
                {
                    if (binOp.Operator.Equals(Operator.Add) && binOp.Right is LiteralBlock && ((LiteralBlock)binOp.Right).Value == "1")
                    {
                        cb.Append("++");
                        cb.AppendAligned(targetCode);
                    }
                    else if ((binOp.Operator.Equals(Operator.Subtract) && binOp.Right is LiteralBlock && ((LiteralBlock)binOp.Right).Value == "1") || (binOp.Operator.Equals(Operator.Add) && binOp.Right is LiteralBlock && ((LiteralBlock)binOp.Right).Value == "-1"))
                    {
                        cb.Append("--");
                        cb.AppendAligned(targetCode);
                    }
                    else
                    {
                        cb.Append(targetCode);
                        cb.Append(' ');
                        cb.Append(binOp.GetOperatorString());
                        cb.Append("= ");
                        cb.AppendAligned(binOp.Right.GetCode());
                    }
                    return cb;
                }
            }
            cb.Append(targetCode);
            cb.Append(" = ");
            if (Value.Type.Equals(Target.Type))
            {
                cb.AppendAligned(Value.GetCode());
            }
            else
            {
                cb.AppendAligned(((ICppBlock)CodeGenerator.EmitConversion(Value, Target.Type)).GetCode());
            }
            return cb;
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
