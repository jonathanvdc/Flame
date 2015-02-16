using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class ILBlockGenerator : IBlockGenerator, IInstruction
    {
        public ILBlockGenerator(ILCodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            instructions = new List<IInstruction>();
        }

        public ILCodeGenerator CodeGenerator { get; private set; }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        private List<IInstruction> instructions;

        public virtual bool IsEmpty
        {
            get 
            {
                return instructions.All((item) => item.IsEmpty);
            }
        }

        public virtual void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            foreach (var item in instructions)
            {
                item.Emit(Context, TypeStack);
            }
        }

        public void EmitBlock(ICodeBlock Block)
        {
            var instr = (IInstruction)Block;
            instructions.Add(instr);
        }

        public void EmitPop(ICodeBlock Block)
        {
            EmitBlock(Block);
            EmitBlock(new PopInstruction(CodeGenerator));
        }

        public void EmitReturn(ICodeBlock Block)
        {
            EmitBlock(Block);
            EmitBlock(new ReturnInstruction(CodeGenerator));
        }

        public void EmitSetField(IField Field, ICodeBlock Target, ICodeBlock Value)
        {
            EmitBlock(new FieldSetInstruction(CodeGenerator, Field, Target, Value));
        }
    }
}
