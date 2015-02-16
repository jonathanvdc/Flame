using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class PartialStackConstructorBlock : IPartialBlock
    {
        public PartialStackConstructorBlock(ICppBlock Constructor)
        {
            this.Constructor = Constructor;
        }

        public ICppBlock Constructor { get; private set; }

        public ICppBlock Complete(PartialArguments Arguments)
        {
            return new StackConstructorBlock(Constructor, Arguments.Arguments);
        }

        public IType Type
        {
            get { return Complete(new PartialArguments()).Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Constructor.Dependencies; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Constructor.LocalsUsed; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Constructor.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            return Constructor.GetCode();
        }
    }
}
