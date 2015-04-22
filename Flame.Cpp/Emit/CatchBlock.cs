using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CatchHeader : ICatchHeader
    {
        public CatchHeader(CppCodeGenerator CodeGenerator, IVariableMember ExceptionVariableMember)
        {
            this.ExceptionVariableDeclaration = new LocalDeclarationReference((CppLocal)CodeGenerator.DeclareVariable(ExceptionVariableMember));
        }

        public LocalDeclarationReference ExceptionVariableDeclaration { get; private set; }
        public IEmitVariable ExceptionVariable { get { return ExceptionVariableDeclaration.Declaration.Local; } }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return ExceptionVariableDeclaration.GetLocalDeclarations(); }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return ExceptionVariableDeclaration.Dependencies; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return ExceptionVariableDeclaration.LocalsUsed; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("catch (");
            cb.Append(ExceptionVariableDeclaration.GetCode());
            cb.Append(")");
            return cb;
        }
    }

    public class CatchBlock : ICatchClause, ICppLocalDeclaringBlock
    {
        public CatchBlock(CatchHeader Header, ICppBlock Body)
        {
            this.Header = Header;
            this.Body = Body;
        }

        public CatchHeader Header { get; private set; }
        public ICppBlock Body { get; private set; }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return Header.LocalDeclarations.Concat(Body.GetLocalDeclarations()); }
        }

        public IType Type
        {
            get { return Body.Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Header.Dependencies.MergeDependencies(Body.Dependencies); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Header.LocalsUsed.Union(Body.LocalsUsed); }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Body.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = Header.GetCode();
            cb.AddEmbracedBodyCodeBuilder(Body.GetCode());
            return cb;
        }

        ICatchHeader ICatchClause.Header
        {
            get { return Header; }
        }
    }
}
