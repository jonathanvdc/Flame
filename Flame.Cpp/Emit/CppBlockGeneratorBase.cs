using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppBlockGeneratorBase : IBlockGenerator, ICppLocalDeclaringBlock
    {
        public CppBlockGeneratorBase(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.blocks = new List<ICppBlock>();
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        protected List<ICppBlock> blocks;

        #region Local Declaration

        private void AddBlock(ICppBlock Block)
        {
            blocks.Add(Block);
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
            get { return DeclarationBlocks.Select((item) => item.Declaration); }
        }

        protected LocalDeclarationReference GetDeclarationBlock(CppLocal Local)
        {
            return DeclarationBlocks.FirstOrDefault((item) => item.Declaration.Local == Local);
        }

        protected void DeclareLocal(CppLocal Local)
        {
            var declBlock = GetDeclarationBlock(Local);
            if (declBlock == null)
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

        protected void ReferenceLocalDeclaration(LocalDeclaration Declaration)
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

        protected void ReferenceLocalDeclarations(IEnumerable<LocalDeclaration> Declarations)
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
            if (!(Block is LocalDeclarationReference))
            {
                if (cppBlock is ICppLocalDeclaringBlock)
                {
                    ReferenceLocalDeclarations(((ICppLocalDeclaringBlock)cppBlock).LocalDeclarations);
                }
                DeclareLocals(cppBlock.LocalsUsed);
            }
            AddBlock(cppBlock);
        }

        public void EmitBreak()
        {
            EmitBlock(new KeywordStatementBlock(CodeGenerator, "break"));
        }

        public void EmitContinue()
        {
            EmitBlock(new KeywordStatementBlock(CodeGenerator, "continue"));
        }

        public void EmitPop(ICodeBlock Block)
        {
            EmitBlock(new ExpressionStatementBlock((ICppBlock)Block));
        }

        public void EmitReturn(ICodeBlock Block)
        {
            EmitBlock(new ReturnBlock(CodeGenerator, (ICppBlock)Block));
        }

        public IType Type
        {
            get
            {
                Stack<IType> types = new Stack<IType>();
                foreach (var item in blocks)
                {
                    var t = item.Type;
                    if (t.Equals(PrimitiveTypes.Void))
                    {
                        types.Push(t);
                    }
                }
                if (types.Count > 0)
                {
                    return types.Pop();
                }
                else
                {
                    return PrimitiveTypes.Void;
                }
            }
        }

        public virtual IEnumerable<IHeaderDependency> Dependencies
        {
            get
            {
                IEnumerable<IHeaderDependency> depends = new IHeaderDependency[0];
                foreach (var item in blocks)
                {
                    depends = depends.MergeDependencies(item.Dependencies);
                }
                return depends;
            }
        }

        public virtual CodeBuilder GetCode()
        {
            if (blocks.Count == 0)
            {
                return new CodeBuilder(";");
            }
            else if (blocks.Count == 1)
            {
                return blocks[0].GetCode();
            }
            else
            {
                CodeBuilder cb = new CodeBuilder();
                cb.AddLine("{");
                cb.IncreaseIndentation();
                foreach (var item in blocks)
                {
                    cb.AddCodeBuilder(item.GetCode());
                }
                cb.DecreaseIndentation();
                cb.AddLine("}");
                return cb;
            }
        }

        public virtual IEnumerable<CppLocal> LocalsUsed
        {
            get { return blocks.SelectMany((item) => item.LocalsUsed).Distinct(); }
        }
    }
}
