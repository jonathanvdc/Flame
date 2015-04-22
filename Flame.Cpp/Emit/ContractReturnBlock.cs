﻿using Flame.Compiler;
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
                    var block = cg.EmitVoid();
                    foreach (var item in Contract.Postconditions)
                    {
                        block = cg.EmitSequence(block, item);
                    }
                    block = cg.EmitSequence(block, new ReturnBlock(cg, null));
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
                    ICodeBlock block = localDecl;
                    foreach (var item in Contract.Postconditions)
                    {
                        block = cg.EmitSequence(block, item);
                    }
                    block = cg.EmitSequence(block, new ReturnBlock(cg, (ICppBlock)cg.ReturnVariable.EmitGet()));
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
                if (Contract.HasPostconditions && localDecl != null)
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
