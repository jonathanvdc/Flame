using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public interface ICppTypeConverter : IConverter<IType, IType>
    {
        /// <summary>
        /// Converts a type with value semantics.
        /// </summary>
        /// <param name="Value"></param>
        /// <returns></returns>
        IType ConvertWithValueSemantics(IType Value);
    }
}
