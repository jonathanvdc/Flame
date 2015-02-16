using Flame.Build;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public interface ICppMember : IMember
    {
        IEnumerable<IHeaderDependency> Dependencies { get; }
        ICppEnvironment Environment { get; }

        CodeBuilder GetHeaderCode();
        bool HasSourceCode { get; }
        CodeBuilder GetSourceCode();
    }

    public interface ICppTemplateMember : ICppMember, IGenericMember, IGenericResolver
    {
        CppTemplateDefinition Templates { get; }
    }
}
