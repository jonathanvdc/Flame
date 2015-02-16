using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class AssignmentBlock : IPythonBlock
    {
        public AssignmentBlock(ICodeGenerator CodeGenerator, IPythonBlock Left, IPythonBlock Right)
        {
            this.CodeGenerator = CodeGenerator;
            this.Left = Left;
            this.Right = Right;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IPythonBlock Left { get; private set; }
        public IPythonBlock Right { get; private set; }

        public CodeBuilder GetCode()
        {
            var cb = new CodeBuilder();
            var leftCode = Left.GetCode();
            cb.Append(leftCode);
            if (Right is BinaryOperation)
            {
                var binaryOp = (BinaryOperation)Right;
                var leftOperandCode = binaryOp.Left.GetCode();
                if (leftOperandCode.ToString() == leftCode.ToString())
                {
                    cb.Append(' ');
                    cb.Append(binaryOp.Operator.ToString());
                    cb.Append("= ");
                    cb.Append(binaryOp.Right.GetCode());
                    return cb;
                }
            }
            cb.Append(" = ");
            cb.Append(Right.GetCode());
            return cb;
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Left.GetDependencies().MergeDependencies(Right.GetDependencies());
        }
    }
}
