using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class AssemblerRegister : IRegister, IConstantStorage
    {
        public AssemblerRegister(ICodeGenerator CodeGenerator, RegisterData Data, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.RegisterType = Data.Kind;
            this.Index = Data.Index;
            this.Type = Type;
        }
        public AssemblerRegister(ICodeGenerator CodeGenerator, RegisterType RegisterType, int Index, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.RegisterType = RegisterType;
            this.Index = Index;
            this.Type = Type;
        }

        public string Identifier
        {
            get
            {
                return RegisterType.GetRegisterName(Index);
            }
        }

        public bool IsTemporary
        {
            get { return RegisterType == Emit.RegisterType.Temporary; }
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public int Index { get; private set; }
        public RegisterType RegisterType { get; private set; }
        public IType Type { get; private set; }

        public RegisterData RegisterData
        {
            get
            {
                return new RegisterData(RegisterType, Index);
            }
        }

        public IAssemblerBlock EmitLoad(IRegister Target)
        {
            //return new RegisterBlock(CodeGenerator, this);
            return new MoveBlock(CodeGenerator, this, Target);
        }

        public IAssemblerBlock EmitStore(IRegister Target)
        {
            return new MoveBlock(CodeGenerator, Target, this);
        }

        public IAssemblerBlock EmitRelease()
        {
            return new ReleaseAssemblerRegisterBlock(this);
        }

        public override string ToString()
        {
            return Identifier;
        }

        public bool IsMutable
        {
            get { return RegisterType != Emit.RegisterType.Zero; }
        }
    }
}
