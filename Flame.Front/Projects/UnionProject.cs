using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    public class UnionProject : IProject
    {
        public UnionProject(IEnumerable<IProject> Projects)
        {
            this.Projects = Projects;
        }

        public IEnumerable<IProject> Projects { get; private set; }

        public string AssemblyName
        {
            get { return Projects.First().AssemblyName; }
        }

        public string BuildTargetIdentifier
        {
            get { return Projects.First().BuildTargetIdentifier; }
        }

        public IProjectItem[] GetChildren()
        {
            return Projects.SelectMany(item => item.GetChildren()).ToArray();
        }

        public string Name
        {
            get { return Projects.First().Name; }
        }
    }
}
