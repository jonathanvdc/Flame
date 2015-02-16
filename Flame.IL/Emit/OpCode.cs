using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public struct OpCode
    {
        public OpCode(byte OpCode)
        {
            this = new OpCode();
            this.Value = OpCode;
        }
        public OpCode(byte OpCode, byte Extension, int DataSize)
        {
            this = new OpCode();
            this.Value = OpCode;
            this.Extension = Extension;
            this.DataSize = DataSize;
        }

        public byte Value { get; private set; }
        public byte Extension { get; private set; }
        public int DataSize { get; private set; }

        public int Size
        {
            get
            {
                return DataSize + (IsExtended ? 2 : 1);
            }
        }

        public bool IsExtended
        {
            get
            {
                return Value == 0xFE;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is OpCode)
            {
                return (OpCode)obj == this;
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return Value << 8 | Extension;
        }

        public static bool operator ==(OpCode A, OpCode B)
        {
            if (A.IsExtended == B.IsExtended)
            {
                if (A.IsExtended)
                {
                    return A.Extension == B.Extension;
                }
                else
                {
                    return A.Value == B.Value;
                }
            }
            else
            {
                return false;
            }
        }
        public static bool operator !=(OpCode A, OpCode B)
        {
            return !(A == B);
        }

        public static OpCode DefineOpCode(byte OpCode)
        {
            OpCode opCode = new OpCode();
            opCode.Value = OpCode;
            return opCode;
        }
        public static OpCode DefineOpCode(byte OpCode, int DataSize)
        {
            OpCode opCode = new OpCode();
            opCode.Value = OpCode;
            opCode.DataSize = DataSize;
            return opCode;
        }
        public static OpCode DefineExtendedOpCode(byte Extension)
        {
            OpCode opCode = new OpCode();
            opCode.Value = 0xFE;
            opCode.Extension = Extension;
            return opCode;
        }
        public static OpCode DefineExtendedOpCode(byte Extension, int DataSize)
        {
            OpCode opCode = new OpCode();
            opCode.Value = 0xFE;
            opCode.Extension = Extension;
            opCode.DataSize = DataSize;
            return opCode;
        }

        public override string ToString()
        {
            return OpCodeConverter.GetOpCodeName(this);
        }
    }
}
