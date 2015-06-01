using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ExpressionStatementBlock : ICppBlock
    {
        public ExpressionStatementBlock(ICppBlock Expression)
        {
            this.Expression = Expression;
        }

        public ICppBlock Expression { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Expression.Dependencies; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Expression.CodeGenerator; }
        }

        private static KeyValuePair<ICppBlock, ICppBlock> PartitionLeftmostBinary(ICppBlock Block, Operator Op)
        {
            if (Block is BinaryOperation)
            {
                var binOp = (BinaryOperation)Block;

                if (binOp.Operator.Equals(Op))
                {
                    var leftmost = PartitionLeftmostBinary(binOp.Left, Op);
                    if (leftmost.Key == null)
	                {
		                return leftmost; // Propagate null.
	                }

                    return new KeyValuePair<ICppBlock, ICppBlock>(leftmost.Key, leftmost.Value != null ? new BinaryOperation(Block.CodeGenerator, leftmost.Value, Op, binOp.Right) : binOp.Right);
                }
                else
                {
                    return new KeyValuePair<ICppBlock, ICppBlock>(null, null);
                }
            }
            else
            {
                return new KeyValuePair<ICppBlock, ICppBlock>(Block, null);
            }
        }

        private CodeBuilder GetExpressionCode()
        {
            if (Expression is BinaryOperation)
            {
                var op = ((BinaryOperation)Expression).Operator;
                var partitioned = PartitionLeftmostBinary(Expression, op);
                return new BinaryOperation(CodeGenerator, partitioned.Key, op, partitioned.Value).GetCode(true);
            }
            else
            {
                return Expression.GetCode();
            }
        }

        public CodeBuilder GetCode()
        {
            var cb = GetExpressionCode();
            if (!cb.IsWhitespace)
            {
                cb.Append(';');
            }
            return cb;
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Expression.LocalsUsed; }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
