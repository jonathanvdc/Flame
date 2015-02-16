using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Options
{
    public interface IBuildParameter
    {
        string Key { get; }
        int ArgumentsCount { get; }
    }
    public interface IBuildOption<out T> : IBuildParameter
    {
        T GetValue(string[] Input);
    }
}
