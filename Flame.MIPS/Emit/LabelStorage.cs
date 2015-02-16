using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class LabelStorage : IStorageLocation
    {
        public LabelStorage(ICodeGenerator CodeGenerator, IAssemblerLabel Address)
        {
            this.CodeGenerator = CodeGenerator;
            this.Label = Address;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IAssemblerLabel Label { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void.MakePointerType(PointerKind.TransientPointer); }
        }

        public IAssemblerBlock EmitLoad(IRegister Target)
        {
            return new ActionAssemblerBlock(CodeGenerator, (context) =>
            {
                context.Emit(new Instruction(OpCodes.LoadAddress, context.ToArgument(Target), context.ToArgument(Label)));
            });
        }

        public IAssemblerBlock EmitStore(IRegister Target)
        {
            throw new InvalidOperationException("Storing a label is illegal.");
        }

        public IAssemblerBlock EmitRelease()
        {
            return new EmptyBlock(CodeGenerator);
        }
    }
}
