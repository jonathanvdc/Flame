using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    public class ProjectOption : IProjectOptionItem
    {
        public ProjectOption(string Key, string Value)
        {
            this.Key = Key;
            this.Value = Value;
        }

        public string Key { get; private set; }

        public string Value { get; private set; }

        public string Name
        {
            get { return "option"; }
        }
    }
}
