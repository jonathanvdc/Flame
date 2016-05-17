using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class LocalBlock : ICppBlock
    {
        public LocalBlock(CppLocal Local)
        {
            this.Local = Local;
        }

        public CppLocal Local { get; private set; }

        public IType Type
        {
            get { return Local.Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return new IHeaderDependency[0]; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return new CppLocal[] { Local }; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Local.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Local.Member.Name.ToString());
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
