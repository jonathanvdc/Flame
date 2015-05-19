using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    public class SingleFileProject : IProject
    {
        public SingleFileProject(ProjectPath Path, string BuildTargetIdentifier)
        {
            this.Path = Path;
            this.BuildTargetIdentifier = BuildTargetIdentifier;
        }

        public ProjectPath Path { get; private set; }
        public string BuildTargetIdentifier { get; private set; }

        public PathIdentifier FilePath { get { return Path.Path; } }

        public string AssemblyName
        {
            get { return Name; }
        }

        private IProjectItem[] projChildren;
        public IProjectItem[] GetChildren()
        {
            if (projChildren == null)
            {
                List<IProjectItem> items = new List<IProjectItem>();
                string[] excluded = new[] { "source", "make-project" };
                foreach (var item in Path.Arguments.Where(item => !excluded.Contains(item.Key)))
                {
                    items.Add(new ProjectOption(item.Key, item.Value.Length == 0 ? "true" : item.Value[0]));
                }
                items.Add(new ProjectNode(new IProjectItem[]
                {
                    new ProjectRuntimeLibrary("PortableRT"),
                    new ProjectRuntimeLibrary("System"),
                    new ProjectRuntimeLibrary("System.Core"),
                }));
                items.Add(new ProjectNode(new IProjectItem[]
                {
                    new ProjectSource(FilePath.AbsolutePath.Path)
                }));
                projChildren = items.ToArray();
            }
            return projChildren;
        }

        public string Name
        {
            get { return FilePath.NameWithoutExtension; }
        }
    }
}
