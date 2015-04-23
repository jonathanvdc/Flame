using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class IfElseBlock : IPythonBlock
    {
        public IfElseBlock(PythonCodeGenerator CodeGenerator, IPythonBlock Condition, IPythonBlock IfBlock, IPythonBlock ElseBlock)
        {
            this.CodeGenerator = CodeGenerator;
            this.IfBlock = IfBlock;
            this.ElseBlock = ElseBlock;
            this.Condition = Condition;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public IPythonBlock Condition { get; private set; }
        public IPythonBlock IfBlock { get; private set; }
        public IPythonBlock ElseBlock { get; private set; }

        public CodeBuilder GetCode()
        {
            return GetCode(false);
        }

        protected CodeBuilder GetCode(bool IsChained)
        {
            CodeBuilder cb = new CodeBuilder();
            if (!IsChained)
            {
                cb.Append("if ");
            }
            else
            {
                cb.Append("elif ");
            }            
            cb.Append(Condition.GetCode());
            cb.Append(':');
            cb.IncreaseIndentation();
            cb.AddBodyCodeBuilder(IfBlock.GetCode());
            cb.DecreaseIndentation();
            if (ElseBlock is IfElseBlock)
            {
                var chainedIf = (IfElseBlock)ElseBlock;
                cb.AddCodeBuilder(chainedIf.GetCode(true));
            }
            else
            {
                var elseBlockCode = ElseBlock.GetCode();
                if (elseBlockCode.LineCount > 0)
                {
                    cb.AddLine("else:");
                    cb.IncreaseIndentation();
                    cb.AddBodyCodeBuilder(ElseBlock.GetCode());
                    cb.DecreaseIndentation();
                }
            }
            return cb;
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return DependencyExtensions.GetDependencies(Condition, IfBlock, ElseBlock);
        }
    }
}
