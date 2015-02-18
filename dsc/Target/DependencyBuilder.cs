using Flame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Target
{
    public class DependencyBuilder : IDependencyBuilder
    {
        public DependencyBuilder(IAssemblyResolver RuntimeLibaryResolver, IEnvironment Environment, string CurrentPath, string OutputFolder)
        {
            this.RuntimeLibaryResolver = RuntimeLibaryResolver;
            this.CurrentPath = CurrentPath;
            this.OutputFolder = OutputFolder;
            this.Environment = Environment;
            this.Properties = Properties;
            this.registeredAssemblies = new List<IAssembly>();
            this.Properties = new TypedDictionary<string>();
        }

        public IAssemblyResolver RuntimeLibaryResolver { get; private set; }
        public IEnvironment Environment { get; private set; }
        public string CurrentPath { get; private set; }
        public string OutputFolder { get; private set; }
        public ITypedDictionary<string> Properties { get; private set; }

        protected Action<IAssembly> AssemblyRegisteredCallback
        {
            get
            {
                return this.GetAssemblyRegisteredCallback();
            }
        }

        private List<IAssembly> registeredAssemblies;

        protected Task<IAssembly> ResolveRuntimeLibraryAsync(string Identifier)
        {
            return RuntimeLibaryResolver.ResolveAsync(Identifier, this);
        }

        protected Task<IAssembly> ResolveReferenceAsync(ReferenceDependency Reference)
        {
            if (Reference.UseCopy)
            {
                return ReferenceResolvers.CopyAndResolveAsync(this, Reference.Identifier, CurrentPath, OutputFolder);
            }
            else
            {
                return ReferenceResolvers.ResolveAsync(this, Reference.Identifier, CurrentPath);
            }
        }

        protected virtual void RegisterAssembly(IAssembly Assembly)
        {
            registeredAssemblies.Add(Assembly);
            var callback = AssemblyRegisteredCallback;
            if (callback != null)
            {
                callback(Assembly);
            }
        }

        private void RegisterAssemblySafe(IAssembly Assembly)
        {
            if (Assembly != null)
            {
                RegisterAssembly(Assembly);
            }
        }

        public async Task AddRuntimeLibraryAsync(string Identifier)
        {
            RegisterAssemblySafe(await ResolveRuntimeLibraryAsync(Identifier));
        }

        public async Task AddReferenceAsync(ReferenceDependency Reference)
        {
            RegisterAssemblySafe(await ResolveReferenceAsync(Reference));
        }

        public IBinder CreateBinder()
        {
            return new MultiBinder(Environment, registeredAssemblies.Select((item) => item.CreateBinder()).ToArray());
        }
    }
}
