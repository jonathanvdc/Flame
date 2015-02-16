using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class NewVectorInstruction : ILInstruction
    {
        public NewVectorInstruction(ICodeGenerator CodeGenerator, IType ElementType, int[] Dimensions)
            : base(CodeGenerator)
        {
            this.ElementType = ElementType;
            this.Dimensions = Dimensions;
        }

        public IType ElementType { get; private set; }
        public int[] Dimensions { get; private set; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            var dims = new IInstruction[Dimensions.Length];
            for (int i = 0; i < dims.Length; i++)
            {
                dims[i] = (IInstruction)CodeGenerator.EmitInt32(Dimensions[i]);
            }

            var arrInstr = new NewArrayInstruction(CodeGenerator, ElementType, dims);
            arrInstr.Emit(Context, TypeStack);

            TypeStack.Pop();
            TypeStack.Push(ElementType.MakeVectorType(Dimensions));
        }
    }
}
