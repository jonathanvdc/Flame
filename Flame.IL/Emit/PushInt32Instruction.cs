using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class PushInt32Instruction : LiteralInstruction<int>
    {
        public PushInt32Instruction(ICodeGenerator CodeGenerator, int Value, IType Type)
            : base(CodeGenerator, Value)
        {
            this.Type = Type;
        }

        public IType Type { get; private set; }

        #region Getting OpCode

        public OpCode OpCode
        {
            get 
            {
                switch (Value)
                {
                    case -1:
                        return OpCodes.LoadInt32_M1;
                    case 0:
                        return OpCodes.LoadInt32_0;
                    case 1:
                        return OpCodes.LoadInt32_1;
                    case 2:
                        return OpCodes.LoadInt32_2;
                    case 3:
                        return OpCodes.LoadInt32_3;
                    case 4:
                        return OpCodes.LoadInt32_4;
                    case 5:
                        return OpCodes.LoadInt32_5;
                    case 6:
                        return OpCodes.LoadInt32_6;
                    case 7:
                        return OpCodes.LoadInt32_7;
                    case 8:
                        return OpCodes.LoadInt32_8;
                    default:
                        break;
                }
                if (Value <= sbyte.MaxValue && Value >= sbyte.MinValue)
                {
                    return OpCodes.LoadInt32Short;
                }
                else
                {
                    return OpCodes.LoadInt32;
                }
            }
        }

        #endregion

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            var opCode = OpCode;
            if (opCode.DataSize == 0)
            {
                Context.Emit(OpCode);
            }
            else if (opCode.DataSize == 1)
            {
                Context.Emit(OpCode, (sbyte)Value);
            }
            else
            {
                Context.Emit(OpCode, Value);
            }
            TypeStack.Push(Type);
        }
    }
}
