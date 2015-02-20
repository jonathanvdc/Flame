using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class BoxConversionBlock : CompositeNewObjectBlockBase
    {
        public BoxConversionBlock(ICppBlock Value)
        {
            this.Value = Value;
        }

        public ICppBlock Value { get; private set; }

        public override ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }

        protected override INewObjectBlock SimplifyNewObject()
        {
            return new ToReferenceBlock(new CopyToPointerBlock(Value));
        }
    }
}
