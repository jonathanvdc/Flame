using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class PartialSharedPtrBlock : IPartialBlock
    {
        public PartialSharedPtrBlock(ICppBlock Constructor)
        {
            this.Constructor = Constructor;
        }

        public ICppBlock Constructor { get; private set; }

        public ICppBlock Complete(PartialArguments Arguments)
        {
            return new SharedPtrBlock(Constructor, Arguments.Arguments);
        }

        public IType Type
        {
            get { return Constructor.Type.MakePointerType(PointerKind.ReferencePointer); }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Constructor.Dependencies.MergeDependencies(new IHeaderDependency[] { new StandardDependency("memory") }); }
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
            return Complete(new PartialArguments()).GetCode();
        }
    }
}
