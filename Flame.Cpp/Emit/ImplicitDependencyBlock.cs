using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ImplicitDependencyBlock : ICppBlock
    {
        public ImplicitDependencyBlock(ICppBlock Block, IEnumerable<IHeaderDependency> ImplictDependencies)
        {
            this.Block = Block;
            this.ImplictDependencies = ImplictDependencies;
        }

        public ICppBlock Block { get; private set; }
        public IEnumerable<IHeaderDependency> ImplictDependencies { get; private set; }

        public ICodeGenerator CodeGenerator { get { return Block.CodeGenerator; } }
        public CodeBuilder GetCode()
        {
            return Block.GetCode();
        }

        public IType Type
        {
            get { return Block.Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Block.Dependencies.MergeDependencies(ImplictDependencies); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Block.LocalsUsed; }
        }
    }
}
