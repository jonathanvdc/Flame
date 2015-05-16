using Flame.Compiler.Projects;
using Flame.Front.Target;
using System.Threading.Tasks;

namespace Flame.Front
{
    public class BinderResolver
    {
        public BinderResolver(IProject Project)
        {
            this.Project = Project;
        }

        public IProject Project { get; private set; }

        public async Task<IBinder> CreateBinderAsync(IDependencyBuilder DependencyBuilder)
        {
            foreach (var item in Project.GetRuntimeLibraryReferences())
            {
                await DependencyBuilder.AddRuntimeLibraryAsync(item);
            } 
            foreach (var item in Project.GetProjectReferences())
            {
                await DependencyBuilder.AddReferenceAsync(new ReferenceDependency(item, true));
            }
            return DependencyBuilder.CreateBinder();
        }
    }
}
