using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class LabelBranchBlock : ICecilBlock
    {
        public LabelBranchBlock(ILLabel Label, ICecilBlock Condition)
        {
            this.Label = Label;
            this.Condition = Condition;
        }

        public ILLabel Label { get; private set; }
        public ICecilBlock Condition { get; private set; }


        public void Emit(IEmitContext Context)
        {
            if (Condition.IsFalseLiteral())
            {
                return; // Do nothing
            }
            var label = Label.GetEmitLabel(Context);
            if (Condition == null || Condition.IsTrueLiteral())
            {
                Context.Emit(OpCodes.Br, label);
            }
            else
            {
                Condition.Emit(Context);
                Context.Stack.Pop();
                var branchOptimizations = new IPeepholeOptimization[]
                {
                    new ConstantBranchOptimization(label),
                    new IsOfTypeBranchOptimization(label),
                    new IsNotOfTypeBranchOptimization(label),
                    new NonNullBranchOptimization(label),
                    new NotComparisonBranchOptimization(label),
                    new BooleanBranchOptimization(label),
                    new ComparisonBranchOptimization(label)
                };
                if (!Context.ApplyAnyOptimization(branchOptimizations))
                {
                    Context.Emit(OpCodes.Brtrue, label);
                }
            }
        }

        public IType BlockType
        {
            get { return PrimitiveTypes.Void; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Label.CodeGenerator; }
        }
    }

    public class JumpTableBlock : ICecilBlock
    {
        public JumpTableBlock(ICecilBlock Index, IReadOnlyList<ILLabel> Labels)
        {
            this.Index = Index;
            this.Labels = Labels;
        }

        public ICecilBlock Index { get; private set; }
        public IReadOnlyList<ILLabel> Labels { get; private set; }

        public void Emit(IEmitContext Context)
        {
            Index.Emit(Context);
            Context.Stack.Pop();
            Context.Emit(OpCodes.Switch, Labels.Select(item => item.GetEmitLabel(Context)).ToArray());
        }

        public IType BlockType
        {
            get { return PrimitiveTypes.Void; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Index.CodeGenerator; }
        }
    }
}
