using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class PropertySetBlock : PropertyBlockBase
    {
        public PropertySetBlock(ICodeGenerator CodeGenerator, IPythonBlock Target, IAccessor Accessor, IPythonBlock Value)
            : base(CodeGenerator, Target, Accessor)
        {
            this.Value = Value;
        }

        public IPythonBlock Value { get; private set; }

        public override IPythonBlock InvocationBlock
        {
            get
            {
                return new InvocationBlock(CodeGenerator, Target, new IPythonBlock[] { Value }, Type);
            }
        }

        public override CodeBuilder GetCode()
        {
            if (Property.UsesPropertySyntax)
            {
                return new AssignmentBlock(CodeGenerator, AccessBlock, Value).GetCode();
            }
            else
            {
                return InvocationBlock.GetCode();
            }
        }

        public override IEnumerable<ModuleDependency> GetDependencies()
        {
            return base.GetDependencies().Union(Value.GetDependencies());
        }
    }
}
