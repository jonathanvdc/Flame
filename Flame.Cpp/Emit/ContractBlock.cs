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
            this.Contract = new MethodContract(Body.CodeGenerator, Preconditions, Postconditions);
        }

        public ICppBlock Body { get; private set; }
        public MethodContract Contract { get; private set; }
        public IEnumerable<PreconditionBlock> Preconditions { get { return Contract.Preconditions; } }
        public IEnumerable<PostconditionBlock> Postconditions { get { return Contract.Postconditions; } }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get 
            {
                return Preconditions.GetLocalDeclarations().Concat(Body.GetLocalDeclarations());
            }
        }

        public IEnumerable<LocalDeclaration> SpilledDeclarations
        {
            get { return Preconditions.GetSpilledLocals().Concat(Body.GetSpilledLocals()); }
        }

        public IType Type
        {
            get { return Body.Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get 
            { 
                return Body.Dependencies.MergeDependencies(Preconditions.GetDependencies())
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
            return new CppBlock(CodeGenerator, this.Flatten().ToArray()).GetCode();
        }

        public IEnumerable<ICppBlock> GetBlocks()
        {
            return Preconditions.With<ICppBlock>(Body);
        }
    }
}
