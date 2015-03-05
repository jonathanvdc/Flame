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
    public class ContractReturnBlock : CompositeBlockBase, ICppLocalDeclaringBlock
    {
        public ContractReturnBlock(IContractCodeGenerator CodeGenerator, MethodContract Contract, ICppBlock ReturnValue)
        {
            this.cg = CodeGenerator;
            this.Contract = Contract;
            this.ReturnValue = ReturnValue;
            if (ReturnValue != null)
            {
                this.localDecl = new LocalDeclarationReference((CppLocal)cg.ReturnVariable, ReturnValue);
            }
        }

        private IContractCodeGenerator cg;
        private LocalDeclarationReference localDecl;
        public MethodContract Contract { get; private set; }
        public ICppBlock ReturnValue { get; private set; }

        protected override bool HasChanged
        {
            get
            {
                return true;
            }
        }

        public override ICodeGenerator CodeGenerator
        {
            get
            {
                return cg;
            }
        }

        public override IType Type
        {
            get
            {
                return PrimitiveTypes.Void;
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
                    block.EmitBlock(localDecl);
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

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get
            {
                if (localDecl != null)
                {
                    return new LocalDeclaration[] { localDecl.Declaration };
                }
                else
                {
                    return Enumerable.Empty<LocalDeclaration>();
                }
            }
        }
    }
}
