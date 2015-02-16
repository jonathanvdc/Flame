using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public interface ICppEnvironment : IEnvironment
    {
        ICppTypeConverter TypeConverter { get; }
        Func<INamespace, IConverter<IType, string>> TypeNamer { get; }
    }
}
