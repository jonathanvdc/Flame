using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class TryBlock : ICppLocalDeclaringBlock
    {
        public TryBlock(ICppBlock Body)
        {
            this.Body = Body;
        }

        public ICppBlock Body { get; private set; }

        public IEnumerable<LocalDeclaration> LocalDeclarations
        {
            get { return Body.GetLocalDeclarations(); }
        }

        public IType Type
        {
            get { return Body.Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Body.Dependencies; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Body.LocalsUsed; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Body.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("try");
            cb.AddEmbracedBodyCodeBuilder(Body.GetCode());
            return cb;
        }
    }
}
