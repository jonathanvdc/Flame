using Flame;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class DependencyBuilder : IDependencyBuilder
    {
        public DependencyBuilder(IAssemblyResolver RuntimeLibaryResolver, IAssemblyResolver ExternalResolver,
            IEnvironment Environment, PathIdentifier CurrentPath, PathIdentifier OutputFolder, ICompilerLog Log)
        {
            this.RuntimeLibaryResolver = RuntimeLibaryResolver;
            this.ExternalResolver = ExternalResolver;
            this.CurrentPath = CurrentPath;
            this.OutputFolder = OutputFolder;
            this.Environment = Environment;
            this.Properties = Properties;
            this.Log = Log;
            this.registeredAssemblies = new List<IAssembly>();
            this.Properties = new TypedDictionary<string>();
        }

        public IAssemblyResolver RuntimeLibaryResolver { get; private set; }
        public IAssemblyResolver ExternalResolver { get; private set; }
        public IEnvironment Environment { get; private set; }
        public ICompilerLog Log { get; private set; }
        public PathIdentifier CurrentPath { get; private set; }
        public PathIdentifier OutputFolder { get; private set; }
        public ITypedDictionary<string> Properties { get; private set; }

        protected Action<IAssembly> AssemblyRegisteredCallback
        {
            get
            {
                return this.GetAssemblyRegisteredCallback();
            }
        }

        private List<IAssembly> registeredAssemblies;

        protected Task<IAssembly> ResolveRuntimeLibraryAsync(PathIdentifier Identifier)
        {
            return RuntimeLibaryResolver.ResolveAsync(Identifier, this);
        }

        protected Task<IAssembly> ResolveReferenceAsync(ReferenceDependency Reference)
        {
            if (Reference.UseCopy)
            {
                return ExternalResolver.CopyAndResolveAsync(Reference.Identifier, CurrentPath, OutputFolder, this);
            }
            else
            {
                return ExternalResolver.ResolveAsync(Reference.Identifier, CurrentPath, this);
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

        public async Task AddRuntimeLibraryAsync(PathIdentifier Identifier)
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
