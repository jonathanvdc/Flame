using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CollectionBlock : ICppBlock, ICollectionBlock
    {
        public CollectionBlock(ICodeGenerator CodeGenerator, IVariableMember Member, ICppBlock Collection)
        {
            this.CodeGenerator = CodeGenerator;
            this.Member = Member;
            this.Collection = Collection;
        }

        public ICppBlock Collection { get; private set; }
        public IVariableMember Member { get; private set; }
        public ICodeGenerator CodeGenerator { get; private set; }

        public bool IsArray
        {
            get
            {
                return Collection.Type.GetIsArray();
            }
        }

        public IType Type
        {
            get { return Member.VariableType; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Collection.Dependencies; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Collection.LocalsUsed; }
        }

        public CodeBuilder GetCode()
        {
            return Collection.GetCode();
        }
    }
}
