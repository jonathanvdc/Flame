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
        // ContractReturnBlock is the ugly mutable duckling in a perfect immutable garden.
        // This entire class is a hack.

        public ContractReturnBlock(CppCodeGenerator CodeGenerator, ICppBlock ReturnValue)
        {
            this.cg = CodeGenerator;
            this.ReturnValue = ReturnValue;
            if (ReturnValue != null)
            {
                this.localDecl = new LocalDeclarationReference((CppLocal)cg.ReturnVariable, ReturnValue);
            }
        }

        private CppCodeGenerator cg;
        private LocalDeclarationReference localDecl;
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
            var contract = cg.Contract;
            if (ReturnValue == null)
            {
                if (contract.HasPostconditions)
                {
                    var block = cg.EmitVoid();
                    foreach (var item in contract.Postconditions)
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
                if (contract.HasPostconditions)
                {
                    ICodeBlock block = localDecl;
                    foreach (var item in contract.Postconditions)
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

        public override IEnumerable<IHeaderDependency> Dependencies
        {
            get
            {
                return cg.Contract.Postconditions.GetDependencies();
            }
        }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get
            {
                /*if (Contract.HasPostconditions && localDecl != null)
                {
                    return new LocalDeclaration[] { localDecl.Declaration };
                }
                else
                {
                    return Enumerable.Empty<LocalDeclaration>();
                }*/
                return Enumerable.Empty<LocalDeclaration>(); // Lie for better results.
            }
        }

        public IEnumerable<LocalDeclaration> SpilledDeclarations
        {
            get { return Enumerable.Empty<LocalDeclaration>(); } // Lie for better results.
        }

        public override IEnumerable<CppLocal> LocalsUsed
        {
            get
            {
                return Enumerable.Empty<CppLocal>(); // Just lie.
            }
        }
    }
}
