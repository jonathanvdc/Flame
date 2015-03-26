using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CatchBlock : ICppLocalDeclaringBlock
    {
        public CatchBlock(LocalDeclarationReference ExceptionVariableDeclaration, ICppBlock Body)
        {
            this.Body = Body;
            this.ExceptionVariableDeclaration = ExceptionVariableDeclaration;
        }

        public ICppBlock Body { get; private set; }
        public LocalDeclarationReference ExceptionVariableDeclaration { get; private set; }
        public IVariable ExceptionVariable { get { return ExceptionVariableDeclaration.Declaration.Local; } }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return Body.GetLocalDeclarations().Concat(ExceptionVariableDeclaration.GetLocalDeclarations()); }
        }

        public IType Type
        {
            get { return Body.Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Body.Dependencies.MergeDependencies(ExceptionVariableDeclaration.Dependencies); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Body.LocalsUsed.Union(ExceptionVariableDeclaration.LocalsUsed); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Body.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("catch (");
            cb.Append(ExceptionVariableDeclaration.GetCode());
            cb.Append(")");
            cb.AddEmbracedBodyCodeBuilder(Body.GetCode());
            return cb;
        }
    }
}
