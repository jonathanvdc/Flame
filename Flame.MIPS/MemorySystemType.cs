using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public sealed class MemorySystemType : StaticSystemTypeBase
    {
        private MemorySystemType()
        { }

        private static MemorySystemType inst;
        public static MemorySystemType Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new MemorySystemType();
                }
                return inst;
            }
        }

        public override string Name
        {
            get { return "Memory"; }
        }

        /*private static IMethod sbrkMethod;
        public static IMethod AllocateMethod
        {
            get
            {
                if (sbrkMethod == null)
                {
                    // static void^ Allocate(int Size);
                    var sbrkTempl = new DescribedMethod("Allocate", Instance, PrimitiveTypes.Void.MakePointerType(PointerKind.ReferencePointer), true);
                    sbrkTempl.AddParameter(new DescribedParameter("Size", PrimitiveTypes.Int32));
                    sbrkMethod = new SyscallMethod(Instance, sbrkTempl, 9);
                }
                return sbrkMethod;
            }
        }*/

        public override IMethod[] GetMethods()
        {
            return new IMethod[]
            {
                AllocateSyscallMethod.GenericInstance
            };
        }
    }
}
