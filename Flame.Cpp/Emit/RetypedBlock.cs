using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class RetypedBlock : ICppBlock
    {
        public RetypedBlock(ICppBlock Block, IType Type)
        {
            this.Block = Block;
            this.Type = Type;
        }

        public ICppBlock Block { get; private set; }
        public IType Type { get; private set; }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Block.Dependencies; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Block.LocalsUsed; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Block.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            return Block.GetCode();
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
