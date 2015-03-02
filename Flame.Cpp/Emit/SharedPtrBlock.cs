using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class SharedPtrBlock : CompositeBlockBase, IPointerBlock, INewObjectBlock
    {
        public SharedPtrBlock(ICppBlock Constructor, IEnumerable<ICppBlock> Arguments)
        {
            this.Constructor = Constructor;
            this.Arguments = Arguments;
        }

        public ICppBlock Constructor { get; private set; }
        public IEnumerable<ICppBlock> Arguments { get; private set; }

        public IMethod Method
        {
            get
            {
                return MethodType.GetMethod(Constructor.Type);
            }
        }

        public ICppBlock StaticDereference()
        {
            return new StackConstructorBlock(Constructor, Arguments);
        }

        public override Compiler.ICodeGenerator CodeGenerator
        {
            get
            {
                return Constructor.CodeGenerator;
            }
        }

        public override ICppBlock Simplify()
        {
            return new ToReferenceBlock(new NewBlock(new StackConstructorBlock(Constructor, Arguments)));
        }

        public AllocationKind Kind
        {
            get { return AllocationKind.ManagedHeap; }
        }
    }
}
