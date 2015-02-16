using Flame.Compiler.Projects;
using Flame.DSProject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Projects
{
    public class SingleFileProject : IProject
    {
        public SingleFileProject(ProjectPath Path)
        {
            this.Path = Path;
        }

        public ProjectPath Path { get; private set; }

        public string FilePath { get { return System.IO.Path.GetFullPath(Path.Path); } }
        public IReadOnlyDictionary<string, string> CompilerOptions { get { return Path.Options; } }

        public string AssemblyName
        {
            get { return Name; }
        }

        public string BuildTargetIdentifier
        {
            get { return Path.Arguments.TargetPlatform; }
        }

        private IProjectItem[] projChildren;
        public IProjectItem[] GetChildren()
        {
            if (projChildren == null)
            {
                List<IProjectItem> items = new List<IProjectItem>();
                foreach (var item in CompilerOptions)
                {
                    items.Add(new DSProjectOption(item.Key, item.Value));
                }
                items.Add(new DSProjectNode(new IProjectItem[]
                {
                    new DSProjectRuntimeLibrary("PortableRT"),
                    new DSProjectRuntimeLibrary("System"),
                    new DSProjectRuntimeLibrary("System.Core"),
                }));
                items.Add(new DSProjectNode(new IProjectItem[]
                {
                    new DSProjectSourceItem(FilePath)
                }));
                projChildren = items.ToArray();
            }
            return projChildren;
        }

        public string Name
        {
            get { return System.IO.Path.GetFileNameWithoutExtension(FilePath); }
        }

        public DSProject ToDSProject()
        {
            return DSProject.FromProject(this, System.IO.Path.ChangeExtension(FilePath, "dsproj"));
        }
    }
}
