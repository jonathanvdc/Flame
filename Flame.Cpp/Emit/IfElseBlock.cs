using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class IfElseBlock : ICppLocalDeclaringBlock, IOpBlock
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
        public int Precedence { get { return 15; } }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get
            {
                return new ICppBlock[] { Condition, IfBlock, ElseBlock }.SelectMany(CppBlockExtensions.GetLocalDeclarations);
            }
        }

        public IEnumerable<LocalDeclaration> SpilledDeclarations
        {
            get { return Enumerable.Empty<LocalDeclaration>(); }
        }

        public IType Type
        {
            get { return !PrimitiveTypes.Void.Equals(IfBlock.Type) && !PrimitiveTypes.Void.Equals(ElseBlock.Type) ? IfBlock.Type : PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Condition.Dependencies.MergeDependencies(IfBlock.Dependencies.MergeDependencies(ElseBlock.Dependencies)); }
        }

        private CodeBuilder GetIfCode()
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

        private CodeBuilder GetTernaryCode()
        {
            var result = Condition.GetOperandCode(this);
            result.Append(" ? ");
            result.AppendAligned(IfBlock.GetOperandCode(this));
            result.Append(" : ");
            result.AppendAligned(ElseBlock.GetOperandCode(this));
            return result;
        }

        public CodeBuilder GetCode()
        {
            if (PrimitiveTypes.Void.Equals(Type))
            {
                return GetIfCode();
            }
            else
            {
                return GetTernaryCode();
            }
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
