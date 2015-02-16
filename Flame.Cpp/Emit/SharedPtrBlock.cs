using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class SharedPtrBlock : CompositeBlockBase, IPointerBlock
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

        protected override ICppBlock Simplify()
        {
            var newBlock = new NewBlock(Constructor);
            var newInvokeBlock = new InvocationBlock(newBlock, Arguments);
            var genericCtor = CppPrimitives.CreateSharedPointer.MakeGenericMethod(new IType[] { Method.DeclaringType });
            return new InvocationBlock(genericCtor.CreateBlock(CodeGenerator), newInvokeBlock);
        }
    }
}
