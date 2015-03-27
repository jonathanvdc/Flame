using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppBlockGeneratorBase : MutableCompositeBlockBase, IBlockGenerator, ICppLocalDeclaringBlock
    {
        public CppBlockGeneratorBase(CppCodeGenerator CodeGenerator)
        {
            this.CppCodeGenerator = CodeGenerator;
            this.blocks = new List<ICppBlock>();
        }

        public CppCodeGenerator CppCodeGenerator { get; private set; }
        protected List<ICppBlock> blocks;

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CppCodeGenerator; }
        }

        #region Local Declaration

        public virtual void AddBlock(ICppBlock Block)
        {
            blocks.Add(Block);
            RegisterChanged();
        }

        public virtual IEnumerable<LocalDeclarationReference> DeclarationBlocks
        {
            get
            {
                return blocks.OfType<LocalDeclarationReference>();
            }
        }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get
            {
                return SimplifiedBlock.GetLocalDeclarations();
            }
        }

        protected LocalDeclarationReference GetDeclarationBlock(CppLocal Local)
        {
            return DeclarationBlocks.FirstOrDefault((item) => item.Declaration.Local == Local);
        }

        protected void DeclareLocal(CppLocal Local)
        {
            if (this.GetDeclarationBlock(Local) == null)
            {
                AddBlock(new LocalDeclarationReference(Local));
            }
        }

        protected void DeclareLocals(IEnumerable<CppLocal> Locals)
        {
            foreach (var item in Locals)
            {
                DeclareLocal(item);
            }
        }

        public void ReferenceLocalDeclaration(LocalDeclaration Declaration)
        {
            var declBlock = GetDeclarationBlock(Declaration.Local);
            if (declBlock != null)
            {
                declBlock.Acquire();
                Declaration.DeclareVariable = false;
            }
            else
            {
                AddBlock(new LocalDeclarationReference(Declaration));
            }
        }

        public void ReferenceLocalDeclarations(IEnumerable<LocalDeclaration> Declarations)
        {
            foreach (var item in Declarations)
            {
                ReferenceLocalDeclaration(item);
            }
        }

        #endregion

        public void EmitBlock(ICodeBlock Block)
        {
            var cppBlock = (ICppBlock)Block;
            if (Block is LocalDeclarationReference)
            {
                var declBlock = (LocalDeclarationReference)Block;
                if (GetDeclarationBlock(declBlock.Declaration.Local) != null) // Don't declare a variable twice
                {
                    declBlock.Hoist();
                }
            }
            else
            {
                var localDecls = cppBlock.GetLocalDeclarations().ToArray();
                ReferenceLocalDeclarations(localDecls);
                var usedLocals = cppBlock.LocalsUsed.ToArray();
                var declUsed = usedLocals.Except(localDecls.Select(item => item.Local)).ToArray();
                DeclareLocals(declUsed);
            }
            AddBlock(cppBlock);
        }

        public void EmitBreak()
        {
            EmitBlock(new KeywordStatementBlock(CppCodeGenerator, "break"));
        }

        public void EmitContinue()
        {
            EmitBlock(new KeywordStatementBlock(CppCodeGenerator, "continue"));
        }

        public void EmitPop(ICodeBlock Block)
        {
            EmitBlock(new ExpressionStatementBlock((ICppBlock)Block));
        }

        public void EmitReturn(ICodeBlock Block)
        {
            EmitBlock(new ContractReturnBlock(CppCodeGenerator, CppCodeGenerator.Contract, Block as ICppBlock));
        }

        public override ICppBlock Simplify()
        {
            return new CppBlock(CppCodeGenerator, blocks);
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
