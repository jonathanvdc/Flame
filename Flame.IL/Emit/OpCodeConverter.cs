using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ILOpCode = System.Reflection.Emit.OpCode;
using ILOpCodes = System.Reflection.Emit.OpCodes;

namespace Flame.IL.Emit
{
    public static class OpCodeConverter
    {
        static OpCodeConverter()
        {
            LoadOpCodes();
        }

        private static ILOpCode[] multiByteOpCodes;
        private static ILOpCode[] singleByteOpCodes;

        private static void LoadOpCodes()
        {
            singleByteOpCodes = new ILOpCode[0x100];
            multiByteOpCodes = new ILOpCode[0x100];
            FieldInfo[] infoArray1 = typeof(ILOpCodes).GetFields();
            for (int num1 = 0; num1 < infoArray1.Length; num1++)
            {
                FieldInfo info1 = infoArray1[num1];
                if (info1.FieldType == typeof(ILOpCode))
                {
                    ILOpCode code1 = (ILOpCode)info1.GetValue(null);
                    ushort num2 = (ushort)code1.Value;
                    if (num2 < 0x100)
                    {
                        singleByteOpCodes[(int)num2] = code1;
                    }
                    else
                    {
                        if ((num2 & 0xff00) != 0xfe00)
                        {
                            throw new Exception("Invalid OpCode.");
                        }
                        multiByteOpCodes[num2 & 0xff] = code1;
                    }
                }
            }
        }

        public static ILOpCode ConvertOpCode(OpCode OpCode)
        {
            if (OpCode.IsExtended)
            {
                return multiByteOpCodes[OpCode.Extension];
            }
            else
            {
                return singleByteOpCodes[OpCode.Value];
            }
        }

        public static string GetOpCodeName(OpCode OpCode)
        {
            return ConvertOpCode(OpCode).Name;
        }
    }
}
