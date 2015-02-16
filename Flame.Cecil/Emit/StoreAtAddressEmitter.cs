using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class StoreAtAddressEmitter : TypedInstructionEmitterBase
    {
        protected override OpCode PointerOpCode
        {
            get { return OpCodes.Stind_I; }
        }

        protected override OpCode Int8OpCode
        {
            get { return OpCodes.Stind_I1; }
        }

        protected override OpCode Int16OpCode
        {
            get { return OpCodes.Stind_I2; }
        }

        protected override OpCode Int32OpCode
        {
            get { return OpCodes.Stind_I4; }
        }

        protected override OpCode Int64OpCode
        {
            get { return OpCodes.Stind_I8; }
        }

        protected override OpCode UInt8OpCode
        {
            get { return DefaultOpCode; }
        }

        protected override OpCode UInt16OpCode
        {
            get { return DefaultOpCode; }
        }

        protected override OpCode UInt32OpCode
        {
            get { return DefaultOpCode; }
        }

        protected override OpCode UInt64OpCode
        {
            get { return DefaultOpCode; }
        }

        protected override OpCode Float32OpCode
        {
            get { return OpCodes.Stind_R4; }
        }

        protected override OpCode Float64OpCode
        {
            get { return OpCodes.Stind_R8; }
        }

        protected override OpCode ObjectOpCode
        {
            get { return OpCodes.Stind_Ref; }
        }

        protected override OpCode DefaultOpCode
        {
            get { return OpCodes.Stobj; }
        }
    }
}
