using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class InitializedConstructorBody : ICppBlock
    {
        public InitializedConstructorBody(ICodeGenerator CodeGenerator, InitializationList Initialization, MethodBodyBlock Body)
        {
            this.CodeGenerator = CodeGenerator;
            this.Initialization = Initialization;
            this.Body = Body;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public InitializationList Initialization { get; private set; }
        public MethodBodyBlock Body { get; private set; }

        public IType Type
        {
            get { return Body.Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return Initialization.Dependencies.MergeDependencies(Body.Dependencies); }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return Initialization.LocalsUsed.Union(Body.LocalsUsed); }
        }

        public CodeBuilder GetCode()
        {
            var cb = Initialization.GetCode();
            cb.AddCodeBuilder(Body.GetCode());
            return cb;
        }
    }
}
