using Flame.CodeDescription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public interface ICppEnvironment : IEnvironment
    {
        DocumentationCommentBuilder DocumentationBuilder { get; }
        ICppTypeConverter TypeConverter { get; }
        Func<INamespace, IConverter<IType, string>> TypeNamer { get; }
        ITypeDefinitionPacker TypeDefinitionPacker { get; }
        TypeDependencyCache DependencyCache { get; }
    }
}
