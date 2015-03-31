using Flame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public interface IDependencyBuilder
    {
        /// <summary>
        /// Adds a reference to a runtime library.
        /// </summary>
        /// <param name="RuntimeLibrary"></param>
        /// <returns></returns>
        Task AddRuntimeLibraryAsync(PathIdentifier Identifier);
        /// <summary>
        /// Adds a reference to an assembly.
        /// </summary>
        /// <param name="Identifier"></param>
        /// <returns></returns>
        Task AddReferenceAsync(ReferenceDependency Reference);

        /// <summary>
        /// Creates a binder for all registered dependencies.
        /// </summary>
        /// <returns></returns>
        IBinder CreateBinder();

        /// <summary>
        /// Gets a types dictionary that describes the dependency builder's properties.
        /// </summary>
        ITypedDictionary<string> Properties { get; }
    }

    public static class DependencyBuilderExtensions
    {
        public static Task AddRuntimeLibraryAsync(this IDependencyBuilder DependencyBuilder, string Identifier)
        {
            return DependencyBuilder.AddRuntimeLibraryAsync(new PathIdentifier(Identifier));
        }

        public static Action<IAssembly> GetAssemblyRegisteredCallback(this IDependencyBuilder DependencyBuilder)
        {
            return DependencyBuilder.Properties.ContainsKey("AssemblyRegisteredCallback") ? DependencyBuilder.Properties.GetValue<Action<IAssembly>>("AssemblyRegisteredCallback") : null;
        }

        private static void SetAssemblyRegisteredCallback(this IDependencyBuilder DependencyBuilder, Action<IAssembly> Callback)
        {
            DependencyBuilder.Properties.SetValue("AssemblyRegisteredCallback", Callback);
        }

        private static ISet<string> GetAssemblyRegisteredCallbackPlatforms(this IDependencyBuilder DependencyBuilder)
        {
            if (!DependencyBuilder.Properties.ContainsKey("AssemblyRegisteredCallbackPlatforms"))
            {
                DependencyBuilder.Properties.SetValue<ISet<string>>("AssemblyRegisteredCallbackPlatforms", new HashSet<string>());
            }
            return DependencyBuilder.Properties.GetValue<ISet<string>>("AssemblyRegisteredCallbackPlatforms");
        }

        private static bool RegisterCallbackPlatform(this IDependencyBuilder DependencyBuilder, string Platform)
        {
            var set = DependencyBuilder.GetAssemblyRegisteredCallbackPlatforms();
            if (set.Contains(Platform))
            {
                return false;
            }
            else
            {
                set.Add(Platform);
                return true;
            }
        }

        public static void AddAssemblyRegisteredCallback(this IDependencyBuilder DependencyBuilder, string Platform, Action<IAssembly> Callback)
        {
            if (RegisterCallbackPlatform(DependencyBuilder, Platform))
            {
                DependencyBuilder.SetAssemblyRegisteredCallback(DependencyBuilder.GetAssemblyRegisteredCallback() + Callback);
            }
        }
    }
}
