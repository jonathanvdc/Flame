using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PartialBinaryOperation : IPartialBlock
    {
        public PartialBinaryOperation(ICodeGenerator CodeGenerator, IType Type, Operator Operator)
        {
            this.CodeGenerator = CodeGenerator;
            this.Type = Type;
            this.Operator = Operator;
        }

        public Operator Operator { get; private set; }
        public IType Type { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public IPythonBlock Complete(IPythonBlock[] Arguments)
        {
            return new BinaryOperation(CodeGenerator, Arguments[0], Operator, Arguments[1]);
        }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Operator.ToString());
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return new ModuleDependency[0];
        }
    }
}
