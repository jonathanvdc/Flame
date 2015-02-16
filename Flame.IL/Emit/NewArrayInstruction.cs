using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class NewArrayInstruction : ILInstruction
    {
        public NewArrayInstruction(ICodeGenerator CodeGenerator, IType ElementType, IInstruction[] Dimensions)
            : base(CodeGenerator)
        {
            this.ElementType = ElementType;
            this.Dimensions = Dimensions;
        }

        public IType ElementType { get; private set; }
        public IInstruction[] Dimensions { get; private set; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            var arrayType = ElementType.MakeArrayType(Dimensions.Length);
            if (Dimensions.Length == 1)
            {
                Dimensions[0].Emit(Context, TypeStack);
                Context.Emit(OpCodes.NewArray, ElementType);
                TypeStack.Push(arrayType);
            }
            else
            {
                var parameters = new IType[Dimensions.Length];
                for (int i = 0; i < parameters.Length; i++)
                {
                    parameters[i] = PrimitiveTypes.Int32;
                }
                var instr = new NewObjectInstruction(CodeGenerator, arrayType.GetConstructors().GetBestMethod(arrayType, parameters), Dimensions);
                instr.Emit(Context, TypeStack);
            }
        }
    }
}
