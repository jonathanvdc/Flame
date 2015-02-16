using Flame.Compiler;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class IncludePath : ISyntaxNode
    {
        public IncludePath(IHeaderDependency Dependency)
        {
            this.Dependency = Dependency;
        }

        public IHeaderDependency Dependency { get; private set; }

        public CodeBuilder GetCode()
        {
            StringBuilder sb = new StringBuilder();
            bool isStandard = Dependency.IsStandard;
            if (isStandard)
            {
                sb.Append('<');
            }
            else
            {
                sb.Append('"');
            }
            sb.Append(Dependency.HeaderName);
            if (isStandard)
            {
                sb.Append('>');
            }
            else
            {
                sb.Append('"');
            }
            return new CodeBuilder(sb.ToString());
        }

        public override string ToString()
        {
            return GetCode().ToString();
        }
    }
}
