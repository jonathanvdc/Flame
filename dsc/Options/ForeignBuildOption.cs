using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Options
{
    /// <summary>
    /// Describes an unknown build option.
    /// </summary>
    public class ForeignBuildOption : IBuildOption<string[]>
    {
        public ForeignBuildOption(string Key, int ArgumentsCount)
        {
            this.Key = Key;
            this.ArgumentsCount = ArgumentsCount;
        }

        public string Key { get; private set; }
        public int ArgumentsCount { get; private set; }

        public string[] GetValue(string[] Input)
        {
            return Input;
        }
    }
}
