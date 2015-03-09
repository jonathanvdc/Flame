using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Options
{
    /// <summary>
    /// The -version option requests dsc to print its version number.
    /// It takes no arguments
    /// </summary>
    public class VersionOption : IBuildOption<bool>
    {
        public bool GetValue(string[] Input)
        {
            return true;
        }

        public string Key
        {
            get { return "version"; }
        }

        public int ArgumentsCount
        {
            get { return 0; }
        }
    }
}
