using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public struct OpCode
    {
        public OpCode(string Name, params InstructionArgumentType[] Parameters)
        {
            this = new OpCode();
            this.Name = Name;
            this.Parameters = Parameters;
        }

        public string Name { get; private set; }
        public InstructionArgumentType[] Parameters { get; private set; }

        #region Equality/Hashing/ToString

        public static bool operator==(OpCode Left, OpCode Right)
        {
            if (object.ReferenceEquals(Left, Right))
            {
                return true;
            }
            else if (object.ReferenceEquals(Left, null) || object.ReferenceEquals(Right, null))
            {
                return false;
            }
            else
            {
                return Left.Name == Right.Name;
            }
        }

        public static bool operator !=(OpCode Left, OpCode Right)
        {
            return !(Left == Right);
        }

        public override bool Equals(object obj)
        {
            if (obj is OpCode)
            {
                return this == (OpCode)obj;
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}
