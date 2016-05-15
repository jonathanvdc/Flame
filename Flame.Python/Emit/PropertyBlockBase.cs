using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public abstract class PropertyBlockBase : IPythonBlock
    {
        public PropertyBlockBase(ICodeGenerator CodeGenerator, IPythonBlock Target, IAccessor Accessor)
        {
            this.CodeGenerator = CodeGenerator;
            this.Accessor = Accessor;
            this.Target = Target;
        }

        public IPythonBlock Target { get; private set; }
        public IAccessor Accessor { get; private set; }
        public IPythonProperty Property { get { return (IPythonProperty)Accessor.DeclaringProperty; } }
        public IType Type { get { return Accessor.ReturnType; } }
        public ICodeGenerator CodeGenerator { get; private set; }

        public MemberAccessBlock AccessBlock
        {
            get
            {
                return new MemberAccessBlock(CodeGenerator, Target, Property.Name.ToString(), Type);
            }
        }
        public abstract IPythonBlock InvocationBlock { get; }

        public virtual CodeBuilder GetCode()
        {
            if (Property.UsesPropertySyntax)
            {
                return AccessBlock.GetCode();
            }
            else
            {
                return InvocationBlock.GetCode();
            }
        }

        public virtual IEnumerable<ModuleDependency> GetDependencies()
        {
            return Target.GetDependencies();
        }
    }
}
