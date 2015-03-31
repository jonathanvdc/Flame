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

        public async Task<IBinder> CreateBinderAsync(BuildTarget Target)
        {
            foreach (var item in Project.GetRuntimeLibraryReferences())
            {
                await Target.DependencyBuilder.AddRuntimeLibraryAsync(item);
            } 
            foreach (var item in Project.GetProjectReferences())
            {
                await Target.DependencyBuilder.AddReferenceAsync(new ReferenceDependency(item, true));
            }
            return Target.DependencyBuilder.CreateBinder();
        }
    }
}
