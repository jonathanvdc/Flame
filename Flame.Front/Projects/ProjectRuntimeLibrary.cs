using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    public class ProjectRuntimeLibrary : IProjectReferenceItem
    {
        public ProjectRuntimeLibrary(string ReferenceIdentifier)
        {
            this.ReferenceIdentifier = ReferenceIdentifier;
        }

        public bool IsRuntimeLibrary
        {
            get { return true; }
        }

        public string ReferenceIdentifier { get; private set; }

        public string Name
        {
            get { return "runtimeLibrary"; }
        }
    }
}
