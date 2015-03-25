using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public abstract class CompositeBlockBase : ICppBlock, IMultiBlock
    {
        private ICppBlock simplified;

        protected virtual bool HasChanged { get { return false; } }
        protected virtual void RegisterSimplified(ICppBlock Block)
        {

        }

        protected ICppBlock SimplifiedBlock
        {
            get
            {
                if (simplified == null || HasChanged)
                {
                    simplified = Simplify();
                    RegisterSimplified(simplified);
                }
                return simplified;
            }
        }

        public abstract ICppBlock Simplify();

        public virtual IType Type
        {
            get { return SimplifiedBlock.Type; }
        }

        public virtual IEnumerable<IHeaderDependency> Dependencies
        {
            get { return SimplifiedBlock.Dependencies; }
        }

        public virtual IEnumerable<CppLocal> LocalsUsed
        {
            get { return SimplifiedBlock.LocalsUsed; }
        }

        public virtual ICodeGenerator CodeGenerator
        {
            get { return SimplifiedBlock.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            return SimplifiedBlock.GetCode();
        }

        public IEnumerable<ICppBlock> GetBlocks()
        {
            return new ICppBlock[] { SimplifiedBlock };
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }

    public abstract class MutableCompositeBlockBase : CompositeBlockBase
    {
        private bool hasChanged;
        protected override bool HasChanged
        {
            get
            {
                return hasChanged;
            }
        }

        protected override void RegisterSimplified(ICppBlock Block)
        {
            hasChanged = false;
        }

        protected virtual void RegisterChanged()
        {
            hasChanged = true;
        }
    }

    public abstract class CompositeInvocationBlockBase : CompositeBlockBase, IInvocationBlock
    {
        protected abstract IInvocationBlock SimplifyInvocation();

        public override ICppBlock Simplify()
        {
            return SimplifyInvocation();
        }

        public IEnumerable<ICppBlock> Arguments
        {
            get { return ((IInvocationBlock)SimplifiedBlock).Arguments; }
        }
    }

    public abstract class CompositeNewObjectBlockBase : CompositeInvocationBlockBase, INewObjectBlock
    {
        protected abstract INewObjectBlock SimplifyNewObject();

        protected override IInvocationBlock SimplifyInvocation()
        {
            return SimplifyNewObject();
        }

        public AllocationKind Kind
        {
            get { return ((INewObjectBlock)SimplifiedBlock).Kind; }
        }
    }
}
