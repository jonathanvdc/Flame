using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front.Options;
using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    /// <summary>
    /// Defines a Flame IR "project", which is really just a
    /// Flame IR assembly wrapped in a convenient format.
    /// </summary>
    public class FlameIRProject : IProject
    {
        public FlameIRProject(ProjectPath Path, IEnumerable<LNode> RootNodes, ICompilerLog Log)
            : this(Path, RootNodes, Log.Options.GetTargetPlatform())
        { }
        public FlameIRProject(ProjectPath Path, IEnumerable<LNode> RootNodes, string BuildTargetIdentifier)
        {
            this.Path = Path;
            this.RootNodes = RootNodes;
            this.BuildTargetIdentifier = BuildTargetIdentifier;
            this.lazyChildren = new Lazy<IProjectItem[]>(createChildren);
            this.lazyName = new Lazy<string>(extractName);
        }

        public ProjectPath Path { get; private set; }
        public IEnumerable<LNode> RootNodes { get; private set; }
        public string BuildTargetIdentifier { get; private set; }

        private Lazy<string> lazyName;
        public string AssemblyName
        {
            get { return lazyName.Value; }
        }

        private string extractName()
        {
            return IRParser.ParseAssemblyName(RootNodes);
        }

        private Lazy<IProjectItem[]> lazyChildren;
        public IProjectItem[] GetChildren()
        {
            return lazyChildren.Value;
        }

        private IProjectItem[] createChildren()
        {
            var items = new List<IProjectItem>();
            foreach (var item in IRParser.ParseRuntimeDependencies(RootNodes))
            {
                items.Add(new ProjectRuntimeLibrary(item));
            }
            foreach (var item in IRParser.ParseLibraryDependencies(RootNodes))
            {
                items.Add(new ProjectLibrary(item));
            }
            items.Add(new ProjectSource(Path.Path.AbsolutePath.Path));
            return items.ToArray();
        }

        public string Name
        {
            get { return AssemblyName; }
        }
    }
}
