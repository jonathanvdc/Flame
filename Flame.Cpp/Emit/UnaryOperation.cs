using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class UnaryOperation : IOpBlock
    {
        public UnaryOperation(ICodeGenerator CodeGenerator, ICppBlock Value, Operator Operator)
            : this(CodeGenerator, Value, Operator, null)
        { }
        public UnaryOperation(ICodeGenerator CodeGenerator, ICppBlock Value, Operator Operator, IMethod OperatorOverload)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
            this.Operator = Operator;
            this.OperatorOverload = OperatorOverload;
        }

        public ICppBlock Value { get; private set; }
        public Operator Operator { get; private set; }
        public int Precedence { get { return 3; } }

        /// <summary>
        /// Gets the operator overload method this operation uses,
        /// if any.
        /// </summary>
        public IMethod OperatorOverload { get; private set; }

        public ICodeGenerator CodeGenerator { get; private set; }

        public CodeBuilder GetCode()
        {
            var cb = new CodeBuilder();
            string opString = GetOperatorString(Operator, Value.Type);
            cb.Append(opString);
            if (opString.Length > 1)
            {
                cb.Append(" ");
            }
            cb.AppendAligned(Value.GetOperandCode(Precedence));
            return cb;
        }

        public static string GetOperatorString(Operator Operator, IType OperandType)
        {
            if (Operator.Equals(Operator.Not))
            {
                if (OperandType.GetIsInteger() || OperandType.GetIsBit())
                    return "~";
                else
                    return "!";
            }
            else
            {
                return Operator.Name;
            }
        }

        public IType Type
        {
            get { return OperatorOverload == null ? Value.Type : OperatorOverload.ReturnType; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Value.LocalsUsed; }
        }

        private IEnumerable<IHeaderDependency> OverloadDependencies
        {
            get { return OperatorOverload == null ? Enumerable.Empty<IHeaderDependency>() : OperatorOverload.GetDependencies(); }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Value.Dependencies.MergeDependencies(OverloadDependencies); }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }

        private static readonly HashSet<Operator> supportedOperators = new HashSet<Operator>()
        {
            Operator.Not, Operator.Subtract, Operator.Hash
        };

        /// <summary>
        /// Tests if the operator is a known unary operator
        /// in C++.
        /// </summary>
        /// <param name="Op"></param>
        /// <returns></returns>
        public static bool IsSupported(Operator Op)
        {
            return supportedOperators.Contains(Op);
        }
    }
}
