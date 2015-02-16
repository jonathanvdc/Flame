using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Options
{
    public class VerifyOption : IBuildOption<bool>
    {
        public bool GetValue(string[] Input)
        {
            return Input[0] == "true";
        }

        public string Key
        {
            get { return "verify"; }
        }

        public int ArgumentsCount
        {
            get { return 1; }
        }
    }
    public class CompileAllOption : IBuildOption<bool>
    {
        public bool GetValue(string[] Input)
        {
            return Input[0] == "true";
        }

        public string Key
        {
            get { return "compileall"; }
        }

        public int ArgumentsCount
        {
            get { return 1; }
        }
    }
    public class MakeProjectBuildOption : IBuildOption<bool>
    {
        public bool GetValue(string[] Input)
        {
            return true;
        }

        public string Key
        {
            get { return "make-project"; }
        }

        public int ArgumentsCount
        {
            get { return 0; }
        }
    }
    public class SingleFileBuildOption : IBuildOption<bool>
    {
        public bool GetValue(string[] Input)
        {
            return true;
        }

        public string Key
        {
            get { return "file"; }
        }

        public int ArgumentsCount
        {
            get { return 0; }
        }
    }
    public class ProjectBuildOption : IBuildOption<bool>
    {
        public bool GetValue(string[] Input)
        {
            return true;
        }

        public string Key
        {
            get { return "project"; }
        }

        public int ArgumentsCount
        {
            get { return 0; }
        }
    }
}
