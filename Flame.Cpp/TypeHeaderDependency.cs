using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class TypeHeaderDependency : IHeaderDependency
    {
        public TypeHeaderDependency(IType Type)
        {
            this.Type = Type;
        }

        public IType Type { get; private set; }

        public bool IsStandard
        {
            get { return false; }
        }

        public string HeaderName
        {
            get { return Type.GetGenericFreeName() + ".h"; }
        }

        public void Include(IOutputProvider OutputProvider)
        {
        }
    }
}
