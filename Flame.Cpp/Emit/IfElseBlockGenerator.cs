using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class IfElseBlockGenerator : IIfElseBlockGenerator, ICppLocalDeclaringBlock
    {
        public IfElseBlockGenerator(ICodeGenerator CodeGenerator, ICppBlock Condition)
        {
            this.CodeGenerator = CodeGenerator;
            this.Condition = Condition;
            this.IfBlock = CodeGenerator.CreateBlock();
            this.ElseBlock = CodeGenerator.CreateBlock();
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public ICppBlock Condition { get; private set; }
        public IBlockGenerator IfBlock { get; private set; }
        public IBlockGenerator ElseBlock { get; private set; }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get 
            {
                return new object[] { Condition, IfBlock, ElseBlock }.OfType<ICppLocalDeclaringBlock>().SelectMany((item) => item.LocalDeclarations);
            }
        }

        public IType Type
        {
            get { return ((ICppBlock)IfBlock).Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Condition.Dependencies.MergeDependencies(((ICppBlock)IfBlock).Dependencies.MergeDependencies(((ICppBlock)ElseBlock).Dependencies)); }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("if (");
            cb.Append(Condition.GetCode());
            cb.Append(")");
            var ifBodyType = cb.AddBodyCodeBuilder(((ICppBlock)IfBlock).GetCode());
            var elseBody = ((ICppBlock)ElseBlock).GetCode();
            if (elseBody.LineCount != 0 && !(elseBody.LineCount == 1 && elseBody[0].Text.Trim() == ";"))
            {
                cb.AppendLine();
                cb.AppendLine("else");
                if (ifBodyType == BodyStatementType.Block)
                {
                    cb.AddEmbracedBodyCodeBuilder(elseBody);
                }
                else
                {
                    cb.AddBodyCodeBuilder(elseBody);
                }
            }
            if (ifBodyType == BodyStatementType.Single)
            {
                cb.AddEmptyLine(); // Add some space for legibility
            }
            return cb;
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Condition.LocalsUsed.Concat(((ICppBlock)IfBlock).LocalsUsed).Concat(((ICppBlock)ElseBlock).LocalsUsed).Distinct(); }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
