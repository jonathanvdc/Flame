using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppBlockGeneratorBase : IBlockGenerator, ICppBlock
    {
        public CppBlockGeneratorBase(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.blocks = new List<ICppBlock>();
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        protected List<ICppBlock> blocks;

        #region Local Declaration

        public IEnumerable<LocalDeclarationBlock> DeclarationBlocks
        {
            get
            {
                return blocks.TakeWhile((item) => item is LocalDeclarationBlock).Cast<LocalDeclarationBlock>();
            }
        }

        public LocalDeclarationBlock FindLocalDeclarationOfType(IType Type)
        {
            return DeclarationBlocks.FirstOrDefault((item) => item.LocalType.Equals(Type));
        }

        protected void DeclareCore(CppLocal Local)
        {
            var localDecl = FindLocalDeclarationOfType(Local.Type);
            if (localDecl == null)
            {
                localDecl = new LocalDeclarationBlock(CodeGenerator, Local);
                blocks.Insert(0, localDecl);
            }
            else
            {
                localDecl.Declare(Local);
            }
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].UsesLocal(Local) && blocks[i] != localDecl)
                {
                    if (blocks[i] is ExpressionStatementBlock)
                    {
                        var expr = ((ExpressionStatementBlock)blocks[i]).Expression;
                        if (expr is VariableAssignmentBlock)
                        {
                            var assignment = (VariableAssignmentBlock)expr;
                            if (assignment.Target is LocalBlock && ((LocalBlock)assignment.Target).Local == Local)
                            {
                                blocks.RemoveAt(i);
                                localDecl.Assign(assignment);
                            }
                        }
                    }
                    break;
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets the single block in this code generator that uses the given local, if any.
        /// </summary>
        /// <param name="Local"></param>
        /// <returns></returns>
        protected ICppBlock GetSingleLocalUsingBlock(CppLocal Local)
        {
            ICppBlock targetBlock = null;
            foreach (var item in blocks)
            {
                if (item.UsesLocal(Local))
                {
                    if (targetBlock != null)
                    {
                        targetBlock = item;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            return targetBlock;
        }

        public void EmitBlock(ICodeBlock Block)
        {
            blocks.Add((ICppBlock)Block);
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
