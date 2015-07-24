using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public struct FlowBlock
    {
        public FlowBlock(BlockTag Tag, Func<Expression> CreateBreak, Func<Expression> CreateContinue)
        {
            this = default(FlowBlock);
            this.Tag = Tag;
            this.CreateBreak = CreateBreak;
            this.CreateContinue = CreateContinue;
        }

        public BlockTag Tag { get; private set; }
        public Func<Expression> CreateBreak { get; private set; }
        public Func<Expression> CreateContinue { get; private set; }

        public static FlowBlock Root
        {
            get
            {
                return new FlowBlock(null, () => { throw new InvalidOperationException("The root flow block does not support 'break'."); }, 
                                           () => { throw new InvalidOperationException("The root flow block does not support 'continue'."); });
            }
        }
    }

    public class FlowStructure
    {
        public FlowStructure(FlowStructure Parent, FlowBlock Flow)
        {
            this.Parent = Parent;
            this.Flow = Flow;
        }

        public FlowStructure Parent { get; private set; }
        public FlowBlock Flow { get; private set; }

        public FlowStructure PushFlow(FlowBlock Flow)
        {
            return new FlowStructure(this, Flow);
        }
        public FlowStructure PushFlow(BlockTag Tag, Func<Expression> CreateBreak, Func<Expression> CreateContinue)
        {
            return PushFlow(new FlowBlock(Tag, CreateBreak, CreateContinue));
        }

        public FlowBlock GetFlow(BlockTag Tag)
        {
            if (Flow.Tag == Tag)
            {
                return Flow;
            }
            else if (IsRoot)
            {
                throw new InvalidProgramException("Could not find a control flow block for block tag '" + Tag.Name + "'.");
            }
            else
            {
                return Parent.GetFlow(Tag);
            }
        }

        public BlockTag FlowTag
        {
            get
            {
                return Flow.Tag;
            }
        }

        public bool IsRoot
        {
            get
            {
                return Parent == null;
            }
        }

        static FlowStructure()
        {
            Root = new FlowStructure(null, FlowBlock.Root);
        }

        public static FlowStructure Root { get; private set; }
    }

    public interface IExpressionBlock : ICodeBlock
    {
        IType Type { get; }
        Expression CreateExpression(FlowStructure Flow);
    }
}
