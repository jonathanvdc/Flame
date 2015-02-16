using Flame.Compiler;
using Flame.MIPS.Static;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class StaticStorage : IUnmanagedStorageLocation
    {
        public StaticStorage(ICodeGenerator CodeGenerator, IStaticDataItem DataItem)
        {
            this.CodeGenerator = CodeGenerator;
            this.DataItem = DataItem;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IStaticDataItem DataItem { get; private set; }
        public IAssemblerLabel Label { get { return DataItem.Label; } }

        public IType Type
        {
            get { return DataItem.Type.MakePointerType(PointerKind.ReferencePointer); }
        }

        public IAssemblerBlock EmitLoad(IRegister Target)
        {
            return new ActionAssemblerBlock(CodeGenerator, (context) =>
            {
                context.Emit(new Instruction(OpCodes.LoadAddress, context.ToArgument(Target), context.ToArgument(Label)));
                var loc = new OffsetRegisterLocation(CodeGenerator, Target, 0, Type);
                loc.EmitLoad(Target).Emit(context);
            });
        }

        public IAssemblerBlock EmitLoadAddress(IRegister Target)
        {
            return new ActionAssemblerBlock(CodeGenerator, (context) =>
            {
                context.Emit(new Instruction(OpCodes.LoadAddress, context.ToArgument(Target), context.ToArgument(Label)));
            });
        }

        public IAssemblerBlock EmitStore(IRegister Target)
        {
            throw new InvalidOperationException("Writing to an address is illegal.");
        }

        public IAssemblerBlock EmitRelease()
        {
            return new EmptyBlock(CodeGenerator);
        }
    }
}
