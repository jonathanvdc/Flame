using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    public class ProjectLibrary : IProjectReferenceItem
    {
        public ProjectLibrary(string ReferenceIdentifier)
        {
            this.ReferenceIdentifier = ReferenceIdentifier;
        }

        public bool IsRuntimeLibrary
        {
            get { return false; }
        }

        public string ReferenceIdentifier { get; private set; }

        public string Name
        {
            get { return "library"; }
        }
    }
}
