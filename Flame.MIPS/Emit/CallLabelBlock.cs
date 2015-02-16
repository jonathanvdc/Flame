using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class CallLabelBlock : IAssemblerBlock
    {
        public CallLabelBlock(ICodeGenerator CodeGenerator, IAssemblerLabel Label, string MethodName)
        {
            this.CodeGenerator = CodeGenerator;
            this.Label = Label;
            this.MethodName = MethodName;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IAssemblerLabel Label { get; private set; }
        public string MethodName { get; private set; }
        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            Context.Emit(new Instruction(OpCodes.JumpAndLink, new IInstructionArgument[] { Context.ToArgument(Label) }, "calls " + MethodName));
            return new IStorageLocation[0];
        }
    }

    public class CallRegisterBlock : IAssemblerBlock
    {
        public CallRegisterBlock(ICodeGenerator CodeGenerator, IRegister Register)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Register;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IRegister Target { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            IRegister callTarget;
            if (Target.RegisterType == RegisterType.AddressRegister)
            {
                callTarget = Context.AllocateRegister(Target.Type);
                Target.EmitStore(callTarget).Emit(Context);
                Target.EmitRelease().Emit(Context);
            }
            else
            {
                callTarget = Target;
            }

            var ra = Context.GetRegister(RegisterType.AddressRegister, 0, PrimitiveTypes.Int32);
            var returnTarget = Context.DeclareLabel("return_target");
            var rtStorage = new LabelStorage(CodeGenerator, returnTarget);
            rtStorage.EmitLoad(ra).Emit(Context);

            Context.Emit(new Instruction(OpCodes.JumpRegister, new IInstructionArgument[] { Context.ToArgument(callTarget) }, "jumps to " + Target.Identifier));
            Context.MarkLabel(returnTarget);
            callTarget.EmitRelease().Emit(Context);

            return new IStorageLocation[0];
        }
    }
}
