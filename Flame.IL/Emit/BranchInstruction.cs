using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public class BranchInstruction : ILInstruction
    {
        public BranchInstruction(ICodeGenerator CodeGenerator, ILLabel Label, ICodeBlock Condition)
            : base(CodeGenerator)
        {
            this.Label = Label;
            this.Condition = (IInstruction)Condition;
        }

        public IInstruction Condition { get; private set; }
        public ILLabel Label { get; private set; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Label.Bind(Context);

            Condition.Emit(Context, TypeStack);

            bool done = true;
            if (Context.CanRemoveTrailingCommands(1))
            {
                var lastCommand = Context.GetLastCommands(1)[0];
                if (lastCommand.OpCode == OpCodes.CheckGreaterThan)
                {
                    Context.RemoveLastCommand();
                    Context.Emit(OpCodes.BranchGreaterThan, Label.EmitLabel);
                }
                else if (lastCommand.OpCode == OpCodes.CheckGreaterThan)
                {
                    Context.RemoveLastCommand();
                    Context.Emit(OpCodes.BranchGreaterThanUnsigned, Label.EmitLabel);
                }
                else if (lastCommand.OpCode == OpCodes.CheckEquals)
                {
                    Context.RemoveLastCommand();
                    if (Context.CanRemoveTrailingCommands(2) && Context.GetLastCommands(2)[0].OpCode == OpCodes.LoadInt32_0)
                    {
                        Context.RemoveLastCommand();
                        if (Context.CanRemoveTrailingCommands(3) && Context.GetLastCommands(3)[0].OpCode == OpCodes.CheckEquals)
                        {
                            Context.RemoveLastCommand();
                            Context.Emit(OpCodes.BranchUnequal, Label.EmitLabel);
                        }
                        else
                        {
                            Context.Emit(OpCodes.BranchFalse, Label.EmitLabel);
                        }
                    }
                    else
                    {
                        Context.Emit(OpCodes.BranchEqual, Label.EmitLabel);
                    }
                }
                else if (lastCommand.OpCode == OpCodes.CheckLessThan)
                {
                    Context.RemoveLastCommand();
                    Context.Emit(OpCodes.BranchLessThan, Label.EmitLabel);
                }
                else if (lastCommand.OpCode == OpCodes.CheckLessThanUnsigned)
                {
                    Context.RemoveLastCommand();
                    Context.Emit(OpCodes.BranchLessThanUnsigned, Label.EmitLabel);
                }
                else if (lastCommand.OpCode == OpCodes.LoadInt32_1)
                {
                    Context.RemoveLastCommand();
                    Context.Emit(OpCodes.Branch, Label.EmitLabel);
                }
                else
                {
                    done = false;
                }
            }
            if (!done)
            {
                Context.Emit(OpCodes.BranchTrue, Label.EmitLabel);
            }

            TypeStack.Pop();
        }
    }
}
