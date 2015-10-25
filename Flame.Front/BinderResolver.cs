using Flame.Compiler.Projects;
using Flame.Front.Projects;
using Flame.Front.Target;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flame.Front
{
    public class PotentialRelativePath
    {
        public PotentialRelativePath(PathIdentifier BasePath, PathIdentifier RelativePath)
        {
            this.BasePath = BasePath;
            this.RelativePath = RelativePath;
            this.AbsolutePath = BasePath.GetAbsolutePath(RelativePath);
        }

        public PathIdentifier BasePath { get; private set; }
        public PathIdentifier RelativePath { get; private set; }
        public PathIdentifier AbsolutePath { get; private set; }

        public override int GetHashCode()
        {
            return AbsolutePath.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is PotentialRelativePath)
            {
                return AbsolutePath == ((PotentialRelativePath)obj).AbsolutePath;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            return "(" + BasePath + ", " + RelativePath + ")";
        }
    }

    public class BinderResolver
    {
        public BinderResolver()
        {
            this.projIdentifiers = new HashSet<PathIdentifier>();
            this.rtLibReferences = new HashSet<PathIdentifier>();
            this.libReferences = new HashSet<PotentialRelativePath>();
        }

        public IEnumerable<PathIdentifier> ProjectIdentifiers { get { return projIdentifiers; } }
        public IEnumerable<PathIdentifier> RuntimeLibraryReferences { get { return rtLibReferences; } }
        public IEnumerable<PathIdentifier> LibraryReferences { get { return libReferences.Select(item => item.RelativePath); } }

        private HashSet<PathIdentifier> projIdentifiers;
        private HashSet<PathIdentifier> rtLibReferences;
        private HashSet<PotentialRelativePath> libReferences;

        public void AddProject(IProject Project, PathIdentifier Identifier)
        {
            this.projIdentifiers.Add(Identifier);

            this.libReferences.UnionWith(Project.GetProjectReferences().Select(item => new PotentialRelativePath(Identifier, new PathIdentifier(item))));
            this.rtLibReferences.UnionWith(Project.GetRuntimeLibraryReferences().Select(PathIdentifier.Parse));

            this.libReferences.ExceptWith(this.projIdentifiers.Select(item => new PotentialRelativePath(item.AbsolutePath, item)));
        }

        public void AddProject(ParsedProject Project)
        {
            AddProject(Project.Project, Project.CurrentPath);
        }

        public static BinderResolver Create(IEnumerable<ParsedProject> Projects)
        {
            var result = new BinderResolver();
            foreach (var item in Projects)
            {
                result.AddProject(item);
            }
            return result;
        }

        public async Task<IBinder> CreateBinderAsync(IDependencyBuilder DependencyBuilder)
        {
            foreach (var item in RuntimeLibraryReferences)
            {
                await DependencyBuilder.AddRuntimeLibraryAsync(item);
            }
            foreach (var item in LibraryReferences)
            {
                await DependencyBuilder.AddReferenceAsync(new ReferenceDependency(item, true));
            }
            return DependencyBuilder.CreateBinder();
        }
    }
}
