using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Options
{
    public interface IOptionParser<in TSource>
    {
        T ParseValue<T>(TSource Value);
        bool CanParse<T>();
    }
}
