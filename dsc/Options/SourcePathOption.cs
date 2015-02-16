using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Options
{
    public class SourcePathOption : IBuildOption<string>
    {
        public string GetValue(string[] Input)
        {
            return Input[0];
        }

        public string Key
        {
            get { return "source"; }
        }

        public int ArgumentsCount
        {
            get { return 1; }
        }
    }
    public class TargetPathOption : IBuildOption<string>
    {
        public string GetValue(string[] Input)
        {
            return Input[0];
        }

        public string Key
        {
            get { return "target"; }
        }

        public int ArgumentsCount
        {
            get { return 1; }
        }
    }
    public class TargetPlatformOption : IBuildOption<string>
    {
        public string GetValue(string[] Input)
        {
            return Input[0];
        }

        public string Key
        {
            get { return "platform"; }
        }

        public int ArgumentsCount
        {
            get { return 1; }
        }
    }
}
