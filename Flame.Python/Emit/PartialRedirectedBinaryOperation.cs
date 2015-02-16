using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PartialRedirectedBinaryOperation : IPartialBlock
    {
        public PartialRedirectedBinaryOperation(ICodeGenerator CodeGenerator, IPythonBlock Left, Operator Operator, IPythonBlock Right)
        {
            this.CodeGenerator = CodeGenerator;
            this.Left = Left;
            this.Operator = Operator;
            this.Right = Right;
        }

        public IPythonBlock Left { get; private set; }
        public Operator Operator { get; private set; }
        public IPythonBlock Right { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public IType Type
        {
            get
            {
                return BinaryOperation.GetResultType(Left, Right, Operator);
            }
        }

        public static IPythonBlock Complete(IPythonBlock Block, IPythonBlock[] Arguments)
        {
            if (Block is IPartialBlock)
            {
                return ((IPartialBlock)Block).Complete(Arguments);
            }
            else
            {
                return Block;
            }
        }

        public IPythonBlock Complete(IPythonBlock[] Arguments)
        {
            return new BinaryOperation(CodeGenerator, Complete(Left, Arguments), Operator, Complete(Right, Arguments));
        }        

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append(Left.GetCode());
            cb.Append(' ');
            cb.Append(Operator.ToString());
            cb.Append(' ');
            cb.Append(Right.GetCode());
            return cb;
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Left.GetDependencies().Union(Right.GetDependencies());
        }
    }
}
