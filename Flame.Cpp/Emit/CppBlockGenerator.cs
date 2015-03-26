using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppBlockGenerator : CppBlockGeneratorBase, IMultiBlock
    {
        public CppBlockGenerator(CppCodeGenerator CodeGenerator)
            : base(CodeGenerator)
        {
        }
        public CppBlockGenerator(CppBlockGenerator Other)
            : base(Other.CppCodeGenerator)
        {
            this.blocks = Other.blocks;
        }

        public virtual CppBlockGenerator ImplyEmptyReturns()
        {
            var blockGen = new CppBlockGenerator(CppCodeGenerator);
            var processedBlocks = new List<ICppBlock>();
            foreach (var item in blocks)
            {
                if (item is CompositeBlockBase)
                {
                    var result = ((CompositeBlockBase)item).Simplify();
                    if (result is CppBlockGenerator)
                    {
                        foreach (var block in ((CppBlockGenerator)result).blocks)
                        {
                            processedBlocks.Add(block);
                        }
                    }
                    else
                    {
                        processedBlocks.Add(result);
                    }
                }
                else
                {
                    processedBlocks.Add(item);
                }
            }

            foreach (var item in processedBlocks)
            {
                if (item is ReturnBlock && ((ReturnBlock)item).Value == null)
                {
                    break;
                }
                else
                {
                    blockGen.blocks.Add(item);
                }
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

        public virtual CppBlockGenerator ImplyStructInit()
        {
            var blockGen = new CppBlockGenerator(CppCodeGenerator);
            foreach (var item in blocks)
            {
                if (!IsStoreThisBlock(item))
                {
                    blockGen.EmitBlock(item);
                }
            }
            return blockGen;
        }

        public IEnumerable<ICppBlock> GetBlocks()
        {
            return blocks;
        }
    }
}
