using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public static class ILBlockExtensions
    {
        public static bool IsTrueLiteral(this ICecilBlock Block)
        {
            if (Block is OpCodeBlock)
            {
                return ((OpCodeBlock)Block).Value == Mono.Cecil.Cil.OpCodes.Ldc_I4_1;
            }
            else
            {
                return false;
            }
        }

        public static bool IsFalseLiteral(this ICecilBlock Block)
        {
            if (Block is OpCodeBlock)
            {
                return ((OpCodeBlock)Block).Value == Mono.Cecil.Cil.OpCodes.Ldc_I4_0;
            }
            else
            {
                return false;
            }
        }

        public static bool IsInt32Literal(this ICecilBlock Block)
        {
            if (Block is OpCodeBlock)
            {
                var op = ((OpCodeBlock)Block).Value;
                return op == Mono.Cecil.Cil.OpCodes.Ldc_I4_0 || op == Mono.Cecil.Cil.OpCodes.Ldc_I4_1 || 
                    op == Mono.Cecil.Cil.OpCodes.Ldc_I4_2 || op == Mono.Cecil.Cil.OpCodes.Ldc_I4_3 || 
                    op == Mono.Cecil.Cil.OpCodes.Ldc_I4_3 || op == Mono.Cecil.Cil.OpCodes.Ldc_I4_4 || 
                    op == Mono.Cecil.Cil.OpCodes.Ldc_I4_5 || op == Mono.Cecil.Cil.OpCodes.Ldc_I4_6 || 
                    op == Mono.Cecil.Cil.OpCodes.Ldc_I4_7 || op == Mono.Cecil.Cil.OpCodes.Ldc_I4_8 || 
                    op == Mono.Cecil.Cil.OpCodes.Ldc_I4_8 || op == Mono.Cecil.Cil.OpCodes.Ldc_I4_M1 ||
                    op == Mono.Cecil.Cil.OpCodes.Ldc_I4_S || op == Mono.Cecil.Cil.OpCodes.Ldc_I4;
            }
            else if (Block is RetypedBlock)
            {
                var convBlock = (RetypedBlock)Block;
                return convBlock.Value.IsInt32Literal();
            }
            else if (Block is ConversionBlock)
            {
                var convBlock = (ConversionBlock)Block;
                return convBlock.Value.IsInt32Literal();
            }
            else
            {
                return false;
            }
        }

        public static int GetInt32Literal(this ICecilBlock Block)
        {
            if (Block is RetypedBlock)
            {
                var convBlock = (RetypedBlock)Block;
                return convBlock.Value.GetInt32Literal();
            }
            else if (Block is ConversionBlock)
            {
                var convBlock = (ConversionBlock)Block;
                return convBlock.Value.GetInt32Literal();
            }
            var block = (OpCodeBlock)Block;
            var op = ((OpCodeBlock)Block).Value;
            if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_0)
            {
                return 0;
            }
            else if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_1)
            {
                return 1;
            }
            else if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_2)
            {
                return 2;
            }
            else if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_3)
            {
                return 3;
            }
            else if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_4)
            {
                return 4;
            }
            else if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_5)
            {
                return 5;
            }
            else if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_6)
            {
                return 6;
            }
            else if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_7)
            {
                return 7;
            }
            else if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_8)
            {
                return 8;
            }
            else if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_M1)
            {
                return -1;
            }
            else if (op == Mono.Cecil.Cil.OpCodes.Ldc_I4_S)
            {
                return ((OpCodeInt16Block)block).Argument;
            }
            else
            {
                return ((OpCodeInt32Block)block).Argument;
            }
        }
    }
}
