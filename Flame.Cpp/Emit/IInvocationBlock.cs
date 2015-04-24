using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    /// <summary>
    /// Provides common functionality for method invocation blocks.
    /// </summary>
    public interface IInvocationBlock : ICppBlock
    {
        IEnumerable<ICppBlock> Arguments { get; }
    }

    public static class InvocationBlockExtensions
    {
        public static CodeBuilder GetArgumentListCode(this IInvocationBlock Block, bool OmitEmptyParentheses)
        {
            var args = Block.Arguments;
            if (!args.Any())
            {
                return OmitEmptyParentheses ? new CodeBuilder() : new CodeBuilder("()");
            }
            else
            {
                var cb = new CodeBuilder();
                cb.Append('(');
                if (args.Any())
                {
                    cb.Append(args.First().GetCode());
                    foreach (var item in args.Skip(1))
                    {
                        cb.Append(", ");
                        cb.Append(item.GetCode());
                    }
                }
                cb.Append(')');
                return cb;
            }
        }

        public static CodeBuilder GetArgumentListCode(this IInvocationBlock Block)
        {
            return Block.GetArgumentListCode(false);
        }

        public static CodeBuilder GetInitializationListCode(this INewObjectBlock Block, bool OmitEmptyParentheses)
        {
            var args = Block.Arguments;
            if (args.Any() && !args.Skip(1).Any())
            {
                var singleArg = args.Single();
                if (singleArg is INewObjectBlock && singleArg.Type.Equals(Block.Type))
                {
                    var initBlock = (INewObjectBlock)singleArg;
                    if (initBlock.Kind == AllocationKind.Stack || initBlock.Kind == AllocationKind.MakeManaged)
                    {
                        if (!OmitEmptyParentheses || initBlock.Arguments.Any())
                        {
                            return initBlock.GetInitializationListCode(OmitEmptyParentheses);
                        }
                        else
                        {
                            return new CodeBuilder();
                        }
                    }
                }
            }
            return Block.GetArgumentListCode(OmitEmptyParentheses);
        }
    }
}
