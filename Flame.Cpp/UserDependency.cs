using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class UserDependency : IHeaderDependency
    {
        public UserDependency(string HeaderName)
        {
            this.HeaderName = HeaderName;
        }

        public bool IsStandard
        {
            get { return false; }
        }

        public string HeaderName { get; private set; }

        public void Include(IOutputProvider OutputProvider)
        {
        }
    }
}
