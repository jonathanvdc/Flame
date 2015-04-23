using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    /*public class CppContractBlockGenerator : CppBlockGenerator, IContractBlockGenerator
    {
        public CppContractBlockGenerator(CppCodeGenerator CodeGenerator, MethodContract Contract)
            : base(CodeGenerator)
        {
            this.Contract = Contract;
        }
        public CppContractBlockGenerator(CppBlockGenerator Other, MethodContract Contract)
            : base(Other)
        {
            this.Contract = Contract;
        }

        public MethodContract Contract { get; private set; }

        public override IEnumerable<IHeaderDependency> Dependencies
        {
            get
            {
                return base.Dependencies
                    .Concat(Contract.Preconditions.SelectMany(item => item.Dependencies)
                    .Concat(Contract.Postconditions.SelectMany(item => item.Dependencies)))
                    .Distinct(HeaderComparer.Instance);
            }
        }

        public void EmitPostcondition(ICodeBlock Block)
        {
            Contract.AddPostcondition(new PostconditionBlock((ICppBlock)Block));
            RegisterChanged();
        }

        public void EmitPrecondition(ICodeBlock Block)
        {
            Contract.AddPrecondition(new PreconditionBlock((ICppBlock)Block));
            RegisterChanged();
        }

        public override ICppBlock Simplify()
        {
            if (Contract.HasPreconditions)
            {
                var newBlocks = new List<ICppBlock>(blocks.Count);
                newBlocks.AddRange(Contract.Preconditions);
                newBlocks.AddRange(blocks);
                return new CppBlock(CppCodeGenerator, newBlocks);
            }
            return base.Simplify();
        }
        
        public override CodeBuilder GetCode()
        {
            return Simplify().GetCode();
        }

        public override CppBlockGenerator ImplyEmptyReturns()
        {
            return new CppContractBlockGenerator(base.ImplyEmptyReturns(), Contract);
        }

        public override CppBlockGenerator ImplyStructInit()
        {
            return new CppContractBlockGenerator(base.ImplyStructInit(), Contract);
        }
    }*/
}
