using Flame.Compiler;
using Flame.Compiler.Code;
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
        public static CodeBuilder GetArgumentListCode(this IInvocationBlock Block, bool OmitEmptyParentheses, int Offset, ICompilerOptions Options)
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
                    int maxLen = Options.get_MaxLineLength() - Offset - 1;
                    int curLen = 0;

                    var argArr = args.ToArray();

                    for (int i = 0; i < argArr.Length; i++)
                    {
                        var argCode = argArr[i].GetCode();
                        if (i < argArr.Length - 1)
                        {
                            argCode.Append(", ");
                        }
                        int argLen = argCode.GetLines().Max(item => item.Length);
                        if (curLen == 0)
                        {
                            curLen = argLen;
                        }
                        else
                        {
                            curLen += argLen;
                            if (curLen > maxLen)
                            {
                                curLen = argLen;
                                cb.AppendLine();
                                cb.Append(" ");
                            }
                        }
                        cb.AppendAligned(1, argCode);
                    }
                }
                cb.Append(')');
                return cb;
            }
        }

        public static CodeBuilder GetArgumentListCode(this IInvocationBlock Block, int Offset, ICompilerOptions Options)
        {
            return Block.GetArgumentListCode(false, Offset, Options);
        }

        public static CodeBuilder GetInitializationListCode(this INewObjectBlock Block, bool OmitEmptyParentheses, int Offset, ICompilerOptions Options)
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
                            return initBlock.GetInitializationListCode(OmitEmptyParentheses, Offset, Options);
                        }
                        else
                        {
                            return new CodeBuilder();
                        }
                    }
                }
            }
            return Block.GetArgumentListCode(OmitEmptyParentheses, Offset, Options);
        }
    }
}
