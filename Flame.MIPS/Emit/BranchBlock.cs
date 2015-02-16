using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class BranchBlock : IAssemblerBlock
    {
        public BranchBlock(AssemblerLateBoundLabel Label, IAssemblerBlock Condition)
        {
            this.Label = Label;
            this.Condition = Condition;
        }

        public IAssemblerBlock Condition { get; private set; }
        public AssemblerLateBoundLabel Label { get; private set; }
        public ICodeGenerator CodeGenerator { get { return Label.CodeGenerator; } }
        public IType Type { get { return PrimitiveTypes.Void; } }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            if (Condition is ConstantBlock)
            {
                var immediate = ((ConstantBlock)Condition).Constant;
                if (immediate != 0)
                {
                    var label = Label.Bind(Context);
                    Context.Emit(new Instruction(OpCodes.Jump, new IInstructionArgument[] { Context.ToArgument(label) }, "jumps to " + label.Identifier));
                }
                // Otherwise, do nothing
            }
            else if (Condition is BinaryOpBlock)
            {
                var binOp = (BinaryOpBlock)Condition;
                if (binOp.Operator.Equals(Operator.CheckEquality))
                {
                    EmitBinaryConditionalBranchInstruction(OpCodes.BranchEqual, binOp, Context);
                }
                else if (binOp.Operator.Equals(Operator.CheckInequality))
                {
                    EmitBinaryConditionalBranchInstruction(OpCodes.BranchNotEqual, binOp, Context);
                }
                else if (binOp.Operator.Equals(Operator.CheckLessThan))
                {
                    EmitBinaryConditionalBranchInstruction(OpCodes.BranchLessThan, binOp, Context);
                }
                else if (binOp.Operator.Equals(Operator.CheckLessThanOrEqual))
                {
                    EmitBinaryConditionalBranchInstruction(OpCodes.BranchLessThanOrEqual, binOp, Context);
                }
                else if (binOp.Operator.Equals(Operator.CheckGreaterThan))
                {
                    EmitBinaryConditionalBranchInstruction(OpCodes.BranchGreaterThan, binOp, Context);
                }
                else if (binOp.Operator.Equals(Operator.CheckGreaterThanOrEqual))
                {
                    EmitBinaryConditionalBranchInstruction(OpCodes.BranchGreaterThanOrEqual, binOp, Context);
                }
                else
                {
                    EmitDefault(Context);
                }
            }
            else if (Condition is NotBlock)
            {
                EmitNot((NotBlock)Condition, Context);
            }
            else
            {
                EmitDefault(Context);
            }
            return new IStorageLocation[0];
        }

        private void EmitBinaryConditionalBranchInstruction(OpCode OpCode, BinaryOpBlock Condition, IAssemblerEmitContext Context)
        {
            var label = Label.Bind(Context);
            var left = Condition.Left.EmitAndSpill(Context);
            var rReg = Condition.Right.EmitToRegister(Context);
            var lReg = left.ReleaseToRegister(Context);

            Context.Emit(new Instruction(OpCode, Context.ToArgument(lReg), Context.ToArgument(rReg), Context.ToArgument(label)));
            lReg.EmitRelease().Emit(Context);
            rReg.EmitRelease().Emit(Context);
        }

        private void EmitNot(NotBlock Block, IAssemblerEmitContext Context)
        {
            var label = Label.Bind(Context);
            var rcond = Block.Value.EmitToRegister(Context);
            Context.Emit(new Instruction(OpCodes.BranchEqual, new IInstructionArgument[] { Context.ToArgument(rcond), Context.ToArgument(Context.GetRegister(RegisterType.Zero, 0, PrimitiveTypes.Boolean)), Context.ToArgument(label) }, "branches to " + label.Identifier));
            rcond.EmitRelease().Emit(Context);
        }

        private void EmitDefault(IAssemblerEmitContext Context)
        {
            var label = Label.Bind(Context);
            var rcond = Condition.EmitToRegister(Context);
            Context.Emit(new Instruction(OpCodes.BranchNotEqual, new IInstructionArgument[] { Context.ToArgument(rcond), Context.ToArgument(Context.GetRegister(RegisterType.Zero, 0, PrimitiveTypes.Boolean)), Context.ToArgument(label) }, "branches to " + label.Identifier));
            rcond.EmitRelease().Emit(Context);
        }
    }
}
