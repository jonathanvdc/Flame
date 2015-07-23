using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ElementAddressOfBlock : ICecilBlock
    {
        public ElementAddressOfBlock(ILElementVariable Variable)
        {
            this.Variable = Variable;
        }

        public ILElementVariable Variable { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Variable.Container.Emit(Context);
            var containerType = Context.Stack.Pop().AsContainerType();
            var elemType = containerType.GetElementType();
            int rank;
            if (containerType.get_IsVector())
            {
                rank = containerType.AsVectorType().GetDimensions().Length;
            }
            else
            {
                rank = containerType.AsArrayType().ArrayRank;
            }
            IType[] argumentTypes = new IType[Variable.Arguments.Length];
            for (int i = 0; i < Variable.Arguments.Length; i++)
            {
                Variable.Arguments[i].Emit(Context);
                argumentTypes[i] = Context.Stack.Pop();
            }
            if (rank == 1)
            {
                Context.Emit(OpCodes.Ldelema, elemType);
            }
            else
            {
                var getMethod = containerType.GetMethod("Address", false, elemType, argumentTypes);
                Context.Emit(OpCodes.Call, getMethod);
            }
            Context.Stack.Push(elemType.MakePointerType(PointerKind.ReferencePointer));
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Variable.CodeGenerator; }
        }


        public IType BlockType
        {
            get { return Variable.Type.MakePointerType(PointerKind.ReferencePointer); }
        }
    }
}
