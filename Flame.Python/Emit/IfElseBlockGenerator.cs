using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python.Emit
{
    public class IfElseBlockGenerator : IIfElseBlockGenerator, IPythonBlock
    {
        public IfElseBlockGenerator(PythonCodeGenerator CodeGenerator, IPythonBlock Condition)
        {
            this.CodeGenerator = CodeGenerator;
            this.IfBlockGenerator = (BlockGenerator)CodeGenerator.CreateBlock();
            this.ElseBlockGenerator = (BlockGenerator)CodeGenerator.CreateBlock();
            this.Condition = Condition;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public IPythonBlock Condition { get; private set; }
        public BlockGenerator IfBlockGenerator { get; private set; }
        public BlockGenerator ElseBlockGenerator { get; private set; }

        public IBlockGenerator ElseBlock
        {
            get { return ElseBlockGenerator; }
        }

        public IBlockGenerator IfBlock
        {
            get { return IfBlockGenerator; }
        }

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
            cb.AddCodeBuilder(IfBlockGenerator.GetBlockCode(true));
            cb.DecreaseIndentation();
            if (ElseBlockGenerator.Children.Count == 1 && ElseBlockGenerator.Children[0] is IfElseBlockGenerator)
            {
                var chainedIf = (IfElseBlockGenerator)ElseBlockGenerator.Children[0];
                cb.AddCodeBuilder(chainedIf.GetCode(true));
            }
            else
            {
                var elseBlockCode = ElseBlockGenerator.GetCode();
                if (elseBlockCode.LineCount > 0)
                {
                    cb.AddLine("else:");
                    cb.IncreaseIndentation();
                    cb.AddCodeBuilder(ElseBlockGenerator.GetCode());
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
            return DependencyExtensions.GetDependencies(Condition, IfBlockGenerator, ElseBlockGenerator);
        }
    }
}
