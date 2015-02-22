using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppBlockGenerator : CppBlockGeneratorBase
    {
        public CppBlockGenerator(ICodeGenerator CodeGenerator)
            : base(CodeGenerator)
        {
        }

        public CppBlockGenerator ImplyEmptyReturns()
        {
            var blockGen = new CppBlockGenerator(CodeGenerator);
            int i;
            for (i = blocks.Count - 1; i >= 0; i--)
            {
                if (!(blocks[i] is ReturnBlock) || ((ReturnBlock)blocks[i]).Value != null)
                {
                    break;
                }
            }
            for (int j = 0; j <= i; j++)
            {
                blockGen.blocks.Add(blocks[j]);
            }
            return blockGen;
        }

        private static bool IsStoreThisBlock(ICppBlock Block)
        {
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

        public CppBlockGenerator ImplyStructInit()
        {
            var blockGen = new CppBlockGenerator(CodeGenerator);
            foreach (var item in blocks)
            {
                if (!IsStoreThisBlock(item))
                {
                    blockGen.EmitBlock(item);
                }
            }
            return blockGen;
        }
    }
}
