using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class LateBoundRegister : IRegister
    {
        public LateBoundRegister(ICodeGenerator CodeGenerator, RegisterData Register, IType Type)
        {
            this.CodeGenerator = CodeGenerator;
            this.Register = Register;
            this.Type = Type;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public RegisterData Register { get; private set; }
        public IType Type { get; private set; }

        private IRegister boundRegister;
        public IRegister Bind(IAssemblerEmitContext Context)
        {
            if (boundRegister == null)
            {
                boundRegister = Context.GetRegister(Register.Kind, Register.Index, Type);
            }
            return boundRegister;
        }

        public string Identifier
        {
            get { return Register.Identifier; }
        }

        public int Index
        {
            get { return Register.Index; }
        }

        public RegisterType RegisterType
        {
            get { return Register.Kind; }
        }

        public bool IsTemporary
        {
            get { return RegisterType == Emit.RegisterType.Temporary; }
        }

        public IAssemblerBlock EmitLoad(IRegister Target)
        {
            return new ActionAssemblerBlock(CodeGenerator, (context) =>
            {
                var reg = Bind(context);
                reg.EmitLoad(Target).Emit(context);
            });
        }

        public IAssemblerBlock EmitStore(IRegister Target)
        {
            return new ActionAssemblerBlock(CodeGenerator, (context) =>
            {
                var reg = Bind(context);
                reg.EmitStore(Target).Emit(context);
            });
        }

        public IAssemblerBlock EmitRelease()
        {
            var reg = boundRegister;
            boundRegister = null;
            if (reg != null)
            {
                return reg.EmitRelease();
            }
            else
            {
                return new EmptyBlock(CodeGenerator);
            }
        }
    }
}
