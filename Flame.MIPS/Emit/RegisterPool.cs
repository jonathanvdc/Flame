using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class RegisterPool
    {
        public RegisterPool()
        {
            this.tempRegisters = new List<RegisterData>();
            this.savedRegisters = new List<RegisterData>();
        }

        private List<RegisterData> tempRegisters;
        private List<RegisterData> savedRegisters;

        public bool SpillRegister
        {
            get
            {
                return tempRegisters.Count < 2;
            }
        }

        public bool SpillToSaved
        {
            get
            {
                return savedRegisters.Count > 4;
            }
        }

        public IEnumerable<RegisterData> FreeRegisters
        {
            get { return tempRegisters.Concat(savedRegisters); }
        }

        public void ReleaseRegister(RegisterData Register)
        {
            if (FreeRegisters.Contains(Register))
            {
                throw new InvalidOperationException("Register " + Register.ToString() + " is released twice.");
            }
            if (Register.Kind == RegisterType.Local)
            {
                this.savedRegisters.Insert(0, Register);
            }
            else
            {
                this.tempRegisters.Insert(0, Register);
            }
        }

        public bool AcquireRegister(RegisterData Data)
        {
            if (tempRegisters.Contains(Data))
            {
                tempRegisters.Remove(Data); 
                return true;
            }
            else if (savedRegisters.Contains(Data))
            {
                savedRegisters.Remove(Data);
                return true;
            }
            else
            {
                return false;
            }
        }

        public RegisterData AllocateRegister()
        {
            if (tempRegisters.Count <= 0)
            {
                throw new InvalidOperationException("Out of temporary registers.");
            }
            var temp = tempRegisters[0];
            tempRegisters.RemoveAt(0);
            return temp;
        }

        public RegisterData AllocateLocal()
        {
            if (savedRegisters.Count <= 0)
            {
                return new RegisterData(RegisterType.Zero, -1);
            }
            var local = savedRegisters[0];
            savedRegisters.RemoveAt(0);
            return local;
        }
    }

    public struct RegisterData
    {
        public RegisterData(RegisterType Kind, int Index)
        {
            this = new RegisterData();
            this.Kind = Kind;
            this.Index = Index;
        }

        public RegisterType Kind { get; private set; }
        public int Index { get; private set; }
        public string Identifier { get { return Kind.GetRegisterName(Index); } }

        public static bool operator ==(RegisterData A, RegisterData B)
        {
            return A.Kind == B.Kind && A.Index == B.Index;
        }
        public static bool operator !=(RegisterData A, RegisterData B)
        {
            return !(A == B);
        }

        public override bool Equals(object obj)
        {
            if (obj is RegisterData)
            {
                return this == (RegisterData)obj;
            }
            else
            {
                return false;
            }
        }
        public override int GetHashCode()
        {
            return Kind.GetHashCode() + Index.GetHashCode();
        }
        public override string ToString()
        {
            return Identifier;
        }
    }
}
