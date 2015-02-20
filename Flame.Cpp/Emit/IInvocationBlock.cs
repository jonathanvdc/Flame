﻿using Flame.Compiler;
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
        public static CodeBuilder GetArgumentListCode(this IInvocationBlock Block)
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append('(');
            var args = Block.Arguments;
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
}
