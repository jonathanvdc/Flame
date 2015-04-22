using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ContractBlock : ICppLocalDeclaringBlock, IMultiBlock
    {
        public ContractBlock(ICppBlock Body, IEnumerable<ICppBlock> Preconditions, IEnumerable<ICppBlock> Postconditions)
        {
            this.Body = Body;
            this.Preconditions = Preconditions;
            this.Postconditions = Postconditions;
        }

        public ICppBlock Body { get; private set; }
        public IEnumerable<ICppBlock> Preconditions { get; private set; }
        public IEnumerable<ICppBlock> Postconditions { get; private set; }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return Body.GetLocalDeclarations()
                             .Concat(Preconditions.Concat(Postconditions).GetLocalDeclarations());
            }
        }

        public IType Type
        {
            get { return Body.Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Body.Dependencies.MergeDependencies(Preconditions.GetDependencies())
                                          .MergeDependencies(Postconditions.GetDependencies());
            }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Body.LocalsUsed.Union(Preconditions.GetUsedLocals()).Union(Postconditions.GetUsedLocals()); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Body.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            return Body.GetCode();
        }

        public IEnumerable<ICppBlock> GetBlocks()
        {
            return new ICppBlock[] { Body };
        }
    }
}
