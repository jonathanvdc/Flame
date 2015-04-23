using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ExceptionHandlingBlock : ICppLocalDeclaringBlock
    {
        public ExceptionHandlingBlock(CppCodeGenerator CodeGenerator, ICppBlock TryBody, ICppBlock FinallyBody, IEnumerable<CatchBlock> CatchClauses)
        {
            this.CodeGenerator = CodeGenerator;
            this.Try = new TryBlock(TryBody);
            this.Finally = new FinallyBlock(FinallyBody);
            this.CatchClauses = CatchClauses;
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public TryBlock Try { get; private set; }
        public FinallyBlock Finally { get; private set; }
        public IEnumerable<CatchBlock> CatchClauses { get; private set; }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return Try.LocalDeclarations.Concat(Finally.LocalDeclarations).Concat(CatchClauses.SelectMany((item) => item.LocalDeclarations)); }
        }

        public IEnumerable<LocalDeclaration> SpilledDeclarations
        {
            get { return Enumerable.Empty<LocalDeclaration>(); }
        }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Try.Dependencies.MergeDependencies(Finally.Dependencies).MergeDependencies(CatchClauses.Aggregate(Enumerable.Empty<IHeaderDependency>(), (a, b) => a.MergeDependencies(b.Dependencies))); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Try.LocalsUsed.Union(Finally.LocalsUsed).Union(CatchClauses.Aggregate(Enumerable.Empty<CppLocal>(), (a, b) => a.Union(b.LocalsUsed))); }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.AddLine("try");
            var tryBodyCode = Try.Body.GetCode();
            var finallyCode = Finally.GetCode();
            if (finallyCode.IsWhitespace)
            {
                cb.AddEmbracedBodyCodeBuilder(tryBodyCode);
            }
            else
            {
                cb.AddEmbracedBodyCodeBuilder(tryBodyCode.PrependStatement(finallyCode));
            }
            foreach (var item in CatchClauses)
            {
                cb.AddCodeBuilder(item.GetCode());
            }
            return cb;
        }
    }
}
