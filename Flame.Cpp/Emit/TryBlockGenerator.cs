using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class TryBlockGenerator : ITryBlockGenerator, ICppLocalDeclaringBlock
    {
        public TryBlockGenerator(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;
            this.TryBody = CodeGenerator.CreateBlock();
            this.FinallyBody = CodeGenerator.CreateBlock();
            this.catchClauses = new List<CatchBlockGenerator>();
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public ICatchBlockGenerator EmitCatchClause(IVariableMember ExceptionVariable)
        {
            var clause = new CatchBlockGenerator(CodeGenerator, ExceptionVariable);
            this.catchClauses.Add(clause);
            return clause;
        }

        public IBlockGenerator TryBody { get; private set; }
        public IBlockGenerator FinallyBody { get; private set; }
        private List<CatchBlockGenerator> catchClauses;

        protected ICppLocalDeclaringBlock CppTryBody
        {
            get { return (ICppLocalDeclaringBlock)TryBody; }
        }
        protected ICppLocalDeclaringBlock CppFinallyBody
        {
            get { return (ICppLocalDeclaringBlock)FinallyBody; }
        }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return CppTryBody.LocalDeclarations.Concat(CppFinallyBody.LocalDeclarations).Concat(catchClauses.SelectMany((item) => item.LocalDeclarations)); }
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return CppTryBody.Dependencies.MergeDependencies(CppFinallyBody.Dependencies).MergeDependencies(catchClauses.Aggregate(Enumerable.Empty<IHeaderDependency>(), (a, b) => a.MergeDependencies(b.Dependencies))); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return CppTryBody.LocalsUsed.Union(CppFinallyBody.LocalsUsed).Union(catchClauses.Aggregate(Enumerable.Empty<CppLocal>(), (a, b) => a.Union(b.LocalsUsed))); }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.AddLine("try");
            cb.AddEmbracedBodyCodeBuilder(CppTryBody.GetCode());
            foreach (var item in catchClauses)
            {
                cb.AddCodeBuilder(item.GetCode());
            }
            var finallyCode = CppFinallyBody.GetCode();
            if (!finallyCode.IsWhitespace && !(finallyCode.LineCount == 1 && finallyCode[0].Text.Trim() == ";"))
            {
                cb.AddLine("finally");
                cb.AddEmbracedBodyCodeBuilder(finallyCode);
            }
            return cb;
        }
    }

    public class CatchBlockGenerator : CppBlockGeneratorBase, ICatchBlockGenerator
    {
        public CatchBlockGenerator(ICodeGenerator CodeGenerator, IVariableMember ExceptionVariableMember)
            : base(CodeGenerator)
        {
            this.ExceptionVariableDeclaration = new LocalDeclarationReference((CppLocal)CodeGenerator.DeclareVariable(ExceptionVariableMember));
        }

        public LocalDeclarationReference ExceptionVariableDeclaration { get; private set; }

        public IVariable ExceptionVariable { get { return ExceptionVariableDeclaration.Declaration.Local; } }

        public override IEnumerable<LocalDeclarationReference> DeclarationBlocks
        {
            get
            {
                return base.DeclarationBlocks.Concat(new LocalDeclarationReference[] { ExceptionVariableDeclaration });
            }
        }

        public override IEnumerable<IHeaderDependency> Dependencies
        {
            get
            {
                return base.Dependencies.MergeDependencies(ExceptionVariableDeclaration.Dependencies);
            }
        }

        public override IEnumerable<CppLocal> LocalsUsed
        {
            get
            {
                return base.LocalsUsed.Union(ExceptionVariableDeclaration.LocalsUsed);
            }
        }

        public override CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("catch (");
            cb.Append(ExceptionVariableDeclaration.GetCode());
            cb.Append(")");
            cb.AddEmbracedBodyCodeBuilder(base.GetCode());
            return cb;
        }
    }
}
