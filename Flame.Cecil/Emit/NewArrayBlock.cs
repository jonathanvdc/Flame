using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class NewArrayBlock : ICecilBlock
    {
        public NewArrayBlock(ICodeGenerator CodeGenerator, IType ElementType, ICecilBlock[] Dimensions)
        {
            this.CodeGenerator = CodeGenerator;
            this.ElementType = ElementType;
            this.Dimensions = Dimensions;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IType ElementType { get; private set; }
        public ICecilBlock[] Dimensions { get; private set; }

        public IType ArrayType
        {
            get
            {
                return ElementType.MakeArrayType(Dimensions.Length);
            }
        }

        public void Emit(IEmitContext Context)
        {
            IType[] dimTypes = new IType[Dimensions.Length];
            for (int i = 0; i < dimTypes.Length; i++)
            {
                Dimensions[i].Emit(Context);
                dimTypes[i] = Context.Stack.Pop();
            }
            if (Dimensions.Length == 1)
            {
                Context.Emit(OpCodes.Newarr, ElementType);
            }
            else
            {
                var ctor = ArrayType.GetConstructor(dimTypes);
                Context.Emit(OpCodes.Newobj, ctor);
            } 
            Context.Stack.Push(ArrayType);
        }

        public IType BlockType
        {
            get { return ArrayType; }
        }
    }
}
