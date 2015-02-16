using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public interface IHeaderDependency
    {
        void Include(IOutputProvider OutputProvider);
        bool IsStandard { get; }
        string HeaderName { get; }
    }
}
