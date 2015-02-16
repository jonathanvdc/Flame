using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class ArgumentBlock : ICppBlock
    {
        public ArgumentBlock(CppArgument Argument)
        {
            this.Argument = Argument;
        }

        public CppArgument Argument { get; private set; }

        public IType Type
        {
            get { return Argument.Type; }
        }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return new IHeaderDependency[0]; }
        }

        public IEnumerable<CppLocal> LocalsUsed
        {
            get { return new CppLocal[0]; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Argument.CodeGenerator; }
        }

        public CodeBuilder GetCode()
        {
            return new CodeBuilder(Argument.Parameter.Name);
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
