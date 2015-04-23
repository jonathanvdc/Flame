using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public static class BodyExtensions
    {
        public static ICppBlock ImplyEmptyReturns(this ICppBlock Block)
        {
            return new CppBlock(Block.CodeGenerator, Block.Flatten().TakeWhile(item => !(item is ReturnBlock && ((ReturnBlock)item).Value == null)).ToArray());
        }

        private static bool IsStoreThisBlock(ICppBlock Block)
        {
            // *this = default(...);
            if (Block is ExpressionStatementBlock)
            {
                var exprStatement = (ExpressionStatementBlock)Block;
                if (exprStatement.Expression is VariableAssignmentBlock)
                {
                    var assignment = (VariableAssignmentBlock)exprStatement.Expression;
                    if (assignment.Target is DereferenceBlock)
                    {
                        var derefPtr = (DereferenceBlock)assignment.Target;
                        return derefPtr.Value is ThisBlock;
                    }
                }
            }
            return false;
        }

        public static ICppBlock ImplyStructInit(this ICppBlock Block)
        {
            return new CppBlock(Block.CodeGenerator, Block.Flatten().Where(item => !IsStoreThisBlock(item)).ToArray());
        }
    }
}
