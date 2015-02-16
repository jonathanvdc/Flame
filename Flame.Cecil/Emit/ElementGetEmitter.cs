using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;

namespace Flame.Cecil.Emit
{
    #region ElementGetEmitter

    public class ElementGetEmitter : TypedInstructionEmitterBase
    {
        protected override OpCode PointerOpCode
        {
            get { return OpCodes.Ldelem_I; }
        }

        protected override OpCode Int8OpCode
        {
            get { return OpCodes.Ldelem_I1; }
        }

        protected override OpCode Int16OpCode
        {
            get { return OpCodes.Ldelem_I2; }
        }

        protected override OpCode Int32OpCode
        {
            get { return OpCodes.Ldelem_I4; }
        }

        protected override OpCode Int64OpCode
        {
            get { return OpCodes.Ldelem_I8; }
        }

        protected override OpCode UInt8OpCode
        {
            get { return OpCodes.Ldelem_U1; }
        }

        protected override OpCode UInt16OpCode
        {
            get { return OpCodes.Ldelem_U2; }
        }

        protected override OpCode UInt32OpCode
        {
            get { return OpCodes.Ldelem_U4; }
        }

        protected override OpCode UInt64OpCode
        {
            get { return DefaultOpCode; }
        }

        protected override OpCode Float32OpCode
        {
            get { return OpCodes.Ldelem_R4; }
        }

        protected override OpCode Float64OpCode
        {
            get { return OpCodes.Ldelem_R8; }
        }

        protected override OpCode ObjectOpCode
        {
            get { return OpCodes.Ldelem_Ref; }
        }

        protected override OpCode DefaultOpCode
        {
            get { return OpCodes.Ldelem_Any; }
        }
    }

    #endregion

    #region ElementSetEmitter

    public class ElementSetEmitter : TypedInstructionEmitterBase
    {
        protected override OpCode PointerOpCode
        {
            get { return OpCodes.Stelem_I; }
        }

        protected override OpCode Int8OpCode
        {
            get { return OpCodes.Stelem_I1; }
        }

        protected override OpCode Int16OpCode
        {
            get { return OpCodes.Stelem_I2; }
        }

        protected override OpCode Int32OpCode
        {
            get { return OpCodes.Stelem_I4; }
        }

        protected override OpCode Int64OpCode
        {
            get { return OpCodes.Stelem_I8; }
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
            get { return OpCodes.Stelem_R4; }
        }

        protected override OpCode Float64OpCode
        {
            get { return OpCodes.Stelem_R8; }
        }

        protected override OpCode ObjectOpCode
        {
            get { return OpCodes.Stelem_Ref; }
        }

        protected override OpCode DefaultOpCode
        {
            get { return OpCodes.Stelem_Any; }
        }
    }

    #endregion
}
