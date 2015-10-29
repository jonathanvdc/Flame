using Flame.Compiler.Projects;
using Flame.Front.Projects;
using Flame.Front.Target;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Flame.Front
{
    public class BinderResolver
    {
        public BinderResolver()
        {
            this.projIdentifiers = new HashSet<PathIdentifier>();
            this.rtLibReferences = new HashSet<PathIdentifier>();
            this.libReferences = new HashSet<PathIdentifier>(PathNameComparer.Instance);
        }

        public IEnumerable<PathIdentifier> ProjectIdentifiers { get { return projIdentifiers; } }
        public IEnumerable<PathIdentifier> RuntimeLibraryReferences { get { return rtLibReferences; } }
        public IEnumerable<PathIdentifier> LibraryReferences { get { return libReferences; } }

        private HashSet<PathIdentifier> projIdentifiers;
        private HashSet<PathIdentifier> rtLibReferences;
        private HashSet<PathIdentifier> libReferences;

        public void AddProject(IProject Project, PathIdentifier Identifier)
        {
            this.projIdentifiers.Add(Identifier);

            this.libReferences.UnionWith(Project.GetProjectReferences().Select(PathIdentifier.Parse));
            this.rtLibReferences.UnionWith(Project.GetRuntimeLibraryReferences().Select(PathIdentifier.Parse));

            this.libReferences.ExceptWith(this.projIdentifiers.Select(item => PathIdentifier.Parse(item.NameWithoutExtension)));
        }

        public void AddProject(ParsedProject Project)
        {
            AddProject(Project.Project, Project.CurrentPath);
        }

        public void AddLibrary(PathIdentifier Identifier)
        {
            var oldIdent = this.libReferences.Where(item => item.NameWithoutExtension == Identifier.NameWithoutExtension || 
                                                            item.Name == Identifier.NameWithoutExtension ||
                                                            item.Name == Identifier.Name).ToArray();
            this.libReferences.ExceptWith(oldIdent);
            this.libReferences.Add(Identifier);
        }

        public void AddRuntimeLibrary(PathIdentifier Identifier)
        {
            this.rtLibReferences.Add(Identifier);
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
            return DependencyBuilder.Binder;
        }
    }
}
