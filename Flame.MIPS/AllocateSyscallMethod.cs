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
        // static T^ Allocate<T>(int Count);
        private AllocateSyscallMethod()
        {
            this.CallConvention = new AutoCallConvention(this, false, true);
        }
        private AllocateSyscallMethod(IType TypeArgument)
        {
            this.CallConvention = new AutoCallConvention(this, false, true);
            this.typeArg = TypeArgument;
        }

        #region Static

        private static AllocateSyscallMethod genericInst;
        public static AllocateSyscallMethod GenericInstance
        {
            get
            {
                if (genericInst == null)
                {
                    genericInst = new AllocateSyscallMethod();
                }
                return genericInst;
            }
        }

        private static IGenericParameter genericParam;
        public static IGenericParameter GenericParameterInstance
        {
            get
            {
                if (genericParam == null)
                {
                    genericParam = new DescribedGenericParameter("T", GenericInstance);
                }
                return genericParam;
            }
        }

        #endregion

        public int ServiceIndex { get { return 9; } }
        public ICallConvention CallConvention { get; private set; }
        private IType typeArg;
        public IType TypeArgument
        {
            get
            {
                return typeArg == null ? GenericParameterInstance : typeArg;
            }
        }

        public IType DeclaringType { get { return MemorySystemType.Instance; } }
        public string Name { get { return "Allocate"; } }
        public string FullName { get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); } }
        public IEnumerable<IAttribute> Attributes
        {
            get
            {
                return new IAttribute[0];
            }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Attributes;
        }

        public IAssemblerBlock CreateCallBlock(ICodeGenerator CodeGenerator)
        {
            int size = TypeArgument.GetSize();
            var block = CodeGenerator.CreateBlock();
            var lbReg = new LateBoundRegister(CodeGenerator, new RegisterData(RegisterType.Argument, 0), PrimitiveTypes.Int32);
            var val = (IAssemblerBlock)CodeGenerator.EmitBinary(new LocationBlock(CodeGenerator, lbReg), CodeGenerator.EmitInt32(size), Operator.Multiply);
            block.EmitBlock(new StoreToBlock(val, lbReg));
            block.EmitBlock(lbReg.EmitRelease());
            block.EmitBlock(new SyscallBlock(CodeGenerator, this));
            return (IAssemblerBlock)block;
        }

        public IMethod[] GetBaseMethods()
        {
            return new IMethod[0];
        }

        public IMethod GetGenericDeclaration()
        {
            return GenericInstance;
        }

        public IParameter[] GetParameters()
        {
            return new IParameter[] { new DescribedParameter("Count", PrimitiveTypes.Int32) };
        }

        public IBoundObject Invoke(IBoundObject Caller, IEnumerable<IBoundObject> Arguments)
        {
            throw new NotImplementedException();
        }

        public bool IsConstructor
        {
            get { return false; }
        }

        public IMethod MakeGenericMethod(IEnumerable<IType> TypeArguments)
        {
            return new AllocateSyscallMethod(TypeArguments.Single());
        }

        public IType ReturnType
        {
            get { return TypeArgument.MakePointerType(PointerKind.ReferencePointer); }
        }

        public bool IsStatic
        {
            get { return true; }
        }

        public IEnumerable<IType> GetGenericArguments()
        {
            if (typeArg == null)
            {
                return new IType[0];
            }
            else
            {
                return new IType[] { TypeArgument };
            }
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return new IGenericParameter[] { GenericParameterInstance };
        }

        public override bool Equals(object obj)
        {
            if (obj is AllocateSyscallMethod)
            {
                return TypeArgument.Equals(((AllocateSyscallMethod)obj).TypeArgument);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ TypeArgument.GetHashCode();
        }
    }
}
