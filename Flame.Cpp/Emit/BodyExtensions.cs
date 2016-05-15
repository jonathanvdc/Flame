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
            // Look for blocks that look like this:
            // *this = ...;

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

        public static MethodBodyBlock OptimizeMethodBody(this ICppBlock Block)
        {
            return new MethodBodyBlock(Block.ImplyEmptyReturns());
        }

        public static InitializedConstructorBody OptimizeConstructorBody(this ICppBlock Block)
        {
            var flattenend = Block.Flatten().ToArray();
            var inits = new List<MemberInitializationBlock>();
            var blocks = new List<ICppBlock>();
            int i;
            for (i = 0; i < flattenend.Length; i++)
            {
                var item = flattenend[i];
                var initBlock = item.ExtractInitializationBlock();
                if (initBlock != null)
                {
                    inits.Add(initBlock);
                }
                else if (item is PreconditionBlock || item is PostconditionBlock)
                {
                    blocks.Add(item);
                }
                else if (!IsStoreThisBlock(item)) // Imply '*this = ...;' blocks
                {
                    break;
                }
            }
            blocks.AddRange(flattenend.Skip(i));

            return new InitializedConstructorBody(Block.CodeGenerator,
                new InitializationList(Block.CodeGenerator, inits),
                new CppBlock(Block.CodeGenerator, blocks).OptimizeMethodBody());
        }

        public static ICppBlock ImplyStructInit(this ICppBlock Block)
        {
            return new CppBlock(Block.CodeGenerator, Block.Flatten().Where(item => !IsStoreThisBlock(item)).ToArray());
        }

        public static MemberInitializationBlock ExtractInitializationBlock(this ICppBlock Block)
        {
            if (Block is ExpressionStatementBlock)
            {
                // Looks for blocks that look like:
                // - this->field = value;
                // - this->base::base(...);

                var exprStatement = (ExpressionStatementBlock)Block;
                if (exprStatement.Expression is VariableAssignmentBlock)
                {
                    var assignment = (VariableAssignmentBlock)exprStatement.Expression;
                    if (assignment.Target is MemberAccessBlock)
                    {
                        var accessBlock = (MemberAccessBlock)assignment.Target;
                        if (accessBlock.Target is ThisBlock)
                        {
                            return new MemberInitializationBlock(
                                accessBlock.CodeGenerator, 
                                new LiteralBlock(
                                    accessBlock.CodeGenerator, 
                                    accessBlock.Member.Name.ToString(), 
                                    accessBlock.Type), 
                                assignment.Value);
                        }
                    }
                }
                else if (exprStatement.Expression is InvocationBlock)
                {
                    var invocation = (InvocationBlock)exprStatement.Expression;
                    if (invocation.Member is MemberAccessBlock)
                    {
                        var accessBlock = (MemberAccessBlock)invocation.Member;
                        if (accessBlock.IsSliceMethod && invocation.Method.IsConstructor)
                        {
                            return new MemberInitializationBlock(
                                accessBlock.CodeGenerator, 
                                new LiteralBlock(
                                    accessBlock.CodeGenerator, 
                                    accessBlock.Member.Name.ToString(), 
                                    accessBlock.Type), 
                                invocation.Arguments);
                        }
                    }
                }
            }
            return null;
        }
    }
}
