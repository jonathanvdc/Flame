using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CopyBlock : CompositeNewObjectBlockBase
    {
        public CopyBlock(ICppBlock Value)
        {
            this.Value = Value;
        }

        public ICppBlock Value { get; private set; }

        public override ICodeGenerator CodeGenerator
        {
            get
            {
                return Value.CodeGenerator;
            }
        }

        protected override INewObjectBlock SimplifyNewObject()
        {
            if (Value is INewObjectBlock && ((INewObjectBlock)Value).Kind == AllocationKind.Stack)
            {
                return (INewObjectBlock)Value;
            }
            else
            {
                return new StackConstructorBlock(Value.Type.GetCopyConstructor().CreateConstructorBlock(CodeGenerator), Value);
            }
        }
    }
}
