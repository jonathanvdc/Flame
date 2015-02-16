using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    /// <summary>
    /// Represents a single MIPS instruction.
    /// </summary>
    public class Instruction : IAssemblerCode
    {
        public Instruction(OpCode OpCode, params IInstructionArgument[] Arguments)
        {
            this.OpCode = OpCode;
            this.Arguments = Arguments;
            this.Comment = string.Empty;
        }
        public Instruction(OpCode OpCode, IReadOnlyList<IInstructionArgument> Arguments)
        {
            this.OpCode = OpCode;
            this.Arguments = Arguments;
            this.Comment = string.Empty;
        }
        public Instruction(OpCode OpCode, IReadOnlyList<IInstructionArgument> Arguments, string Comment)
        {
            this.OpCode = OpCode;
            this.Arguments = Arguments;
            this.Comment = Comment;
        }

        public OpCode OpCode { get; private set; }
        public IReadOnlyList<IInstructionArgument> Arguments { get; private set; }
        public string Comment { get; private set; }

        /// <summary>
        /// Checks the correctness of the function's parameters.
        /// </summary>
        /// <returns></returns>
        public bool Verify()
        {
            if (Arguments.Count != OpCode.Parameters.Length)
            {
                return false;
            }
            for (int i = 0; i < OpCode.Parameters.Length; i++)
            {
                if ((Arguments[i].ArgumentType & OpCode.Parameters[i]) == default(InstructionArgumentType))
                {
                    return false;
                }
            }
            return true;
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append(OpCode.Name);
            bool first = true;
            foreach (var item in Arguments)
            {
                if (first)
                {
                    first = false;
                    cb.Append(' ');
                }
                else
                {
                    cb.Append(", ");
                }
                cb.Append(item.GetCode());
            }
            if (!string.IsNullOrWhiteSpace(Comment))
            {
                cb.Append(" # ");
                cb.Append(Comment);
            }
            return cb;
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
