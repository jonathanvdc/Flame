using Flame.Build;
using Flame.Compiler;
using Flame.MIPS.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public sealed class AllocateSyscallMethod : ISyscallMethod
    {
        // static void* Allocate(int Size);
        private AllocateSyscallMethod()
        {
            this.CallConvention = new AutoCallConvention(this, false, true);
        }

        #region Static

        private static AllocateSyscallMethod inst;
        public static AllocateSyscallMethod Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new AllocateSyscallMethod();
                }
                return inst;
            }
        }

        #endregion

        public int ServiceIndex { get { return 9; } }
        public ICallConvention CallConvention { get; private set; }

        public IType DeclaringType { get { return MemorySystemType.Instance; } }
        public UnqualifiedName Name { get { return new SimpleName("Allocate"); } }
        public QualifiedName FullName { get { return Name.Qualify(DeclaringType.FullName); } }
        public AttributeMap Attributes
        {
            get
            {
                return AttributeMap.Empty;
            }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Attributes;
        }

        public IAssemblerBlock CreateCallBlock(ICodeGenerator CodeGenerator)
        {
            /*
            var lbReg = new LateBoundRegister(CodeGenerator, new RegisterData(RegisterType.Argument, 0), PrimitiveTypes.Int32);
            var val = (IAssemblerBlock)CodeGenerator.EmitBinary(new LocationBlock(CodeGenerator, lbReg), CodeGenerator.EmitInt32(size), Operator.Multiply);
            return (IAssemblerBlock)CodeGenerator.EmitSequence(new StoreToBlock(val, lbReg), CodeGenerator.EmitSequence(lbReg.EmitRelease(), new SyscallBlock(CodeGenerator, this)));
            */

            return new SyscallBlock(CodeGenerator, this);
        }

        public IEnumerable<IMethod> BaseMethods
        {
            get { return new IMethod[0]; }
        }

        public IEnumerable<IParameter> Parameters
        {
            get { return new IParameter[] { new DescribedParameter("Count", PrimitiveTypes.Int32) }; }
        }

        public bool IsConstructor
        {
            get { return false; }
        }

        public IType ReturnType
        {
            get { return PrimitiveTypes.Void.MakePointerType(PointerKind.TransientPointer); }
        }

        public bool IsStatic
        {
            get { return true; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return new IGenericParameter[0]; }
        }
    }
}
