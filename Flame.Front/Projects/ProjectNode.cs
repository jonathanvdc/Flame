using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    public class ProjectNode : IProjectNode
    {
        public ProjectNode(IProjectItem[] Children)
        {
            this.Name = string.Empty;
            this.Children = Children;
        }
        public ProjectNode(string Name, IProjectItem[] Children)
        {
            this.Name = Name;
            this.Children = Children;
        }

        public string Name { get; private set; }
        public IProjectItem[] Children { get; private set; }

        public IProjectItem[] GetChildren()
        {
            return Children;
        }
    }
}
