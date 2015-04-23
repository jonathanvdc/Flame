using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class IfElseBlock : ICppLocalDeclaringBlock
    {
        public IfElseBlock(ICodeGenerator CodeGenerator, ICppBlock Condition,
                           ICppBlock IfBlock, ICppBlock ElseBlock)
        {
            this.CodeGenerator = CodeGenerator;
            this.Condition = Condition;
            this.IfBlock = IfBlock;
            this.ElseBlock = ElseBlock;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICppBlock Condition { get; private set; }
        public ICppBlock IfBlock { get; private set; }
        public ICppBlock ElseBlock { get; private set; }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get
            {
                return new object[] { Condition, IfBlock, ElseBlock }.OfType<ICppLocalDeclaringBlock>().SelectMany((item) => item.LocalDeclarations);
            }
        }

        public IEnumerable<LocalDeclaration> SpilledDeclarations
        {
            get { return Enumerable.Empty<LocalDeclaration>(); }
        }

        public IEnumerable<LocalDeclaration> CommonDeclarations
        {
            get
            {
                var condDecls = Condition.GetLocalDeclarations();
                var ifDecls = IfBlock.GetLocalDeclarations();
                var elseDecls = ElseBlock.GetLocalDeclarations();
                return condDecls.Intersect(ifDecls)
                    .Union(condDecls.Intersect(elseDecls))
                    .Union(ifDecls.Intersect(elseDecls));
            }
        }

        public IType Type
        {
            get { return IfBlock.Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Condition.Dependencies.MergeDependencies(IfBlock.Dependencies.MergeDependencies(ElseBlock.Dependencies)); }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("if (");
            cb.Append(Condition.GetCode());
            cb.Append(")");
            var ifBody = IfBlock.GetCode();
            BodyStatementType ifBodyType;
            if (ifBody.FirstCodeLine.Text.TrimStart().StartsWith("if")) // We don't want a dangling else
            {
                cb.AddEmbracedBodyCodeBuilder(ifBody);
                ifBodyType = BodyStatementType.Block;
            }
            else
            {
                ifBodyType = cb.AddBodyCodeBuilder(ifBody);
            }
            bool appendEmptyLine = ifBodyType == BodyStatementType.Single;
            var elseBody = ElseBlock.GetCode();
            if (elseBody.LineCount > 0 && !(elseBody.CodeLineCount == 1 && elseBody.FirstCodeLine.Text.Trim() == ";"))
            {
                cb.AddLine("else");
                if (elseBody.FirstCodeLine.Text.TrimStart().StartsWith("if"))
                {
                    cb.Append(" ");
                    cb.Append(elseBody);
                    appendEmptyLine = false;
                }
                else if (ifBodyType == BodyStatementType.Block)
                {
                    cb.AddEmbracedBodyCodeBuilder(elseBody);
                }
                else
                {
                    cb.AddBodyCodeBuilder(elseBody);
                }
            }
            if (appendEmptyLine)
            {
                cb.AddEmptyLine(); // Add some space for legibility
            }
            return cb;
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Condition.LocalsUsed.Concat(IfBlock.LocalsUsed).Concat(ElseBlock.LocalsUsed).Distinct(); }
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
