using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ContractReturnBlock : CompositeBlockBase
    {
        public ContractReturnBlock(IContractCodeGenerator CodeGenerator, MethodContract Contract, ICppBlock ReturnValue)
        {
            this.cg = CodeGenerator;
            this.Contract = Contract;
            this.ReturnValue = ReturnValue;
        }

        private IContractCodeGenerator cg;
        public MethodContract Contract { get; private set; }
        public ICppBlock ReturnValue { get; private set; }

        public override ICodeGenerator CodeGenerator
        {
            get
            {
                return cg;
            }
        }

        public override ICppBlock Simplify()
        {
            if (ReturnValue == null)
            {
                if (Contract.HasPostconditions)
                {
                    var block = cg.CreateBlock();
                    foreach (var item in Contract.Postconditions)
                    {
                        block.EmitBlock(item);
                    }
                    block.EmitBlock(new ReturnBlock(cg, null));
                    return (ICppBlock)block;
                }
                else
                {
                    return new ReturnBlock(cg, null);
                }
            }
            else
            {
                if (Contract.HasPostconditions)
                {
                    var block = cg.CreateBlock();
                    cg.ReturnVariable.CreateSetStatement(new CodeBlockExpression(ReturnValue, ReturnValue.Type)).Emit(block);
                    foreach (var item in Contract.Postconditions)
                    {
                        block.EmitBlock(item);
                    }
                    block.EmitBlock(new ReturnBlock(cg, (ICppBlock)cg.ReturnVariable.CreateGetExpression().Emit(cg)));
                    return (ICppBlock)block;
                }
                else
                {
                    return new ReturnBlock(cg, ReturnValue);
                }
            }
        }
    }
}
