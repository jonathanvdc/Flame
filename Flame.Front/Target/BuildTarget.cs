using Flame.Front.Options;
using Flame.Front.Plugs;
using Flame;
using Flame.Compiler;
using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class BuildTarget
    {
        public BuildTarget(IAssemblyBuilder TargetAssembly, IAssemblyResolver RuntimeLibraryResolver, IDependencyBuilder DependencyBuilder, string Extension)
        {
            this.TargetAssembly = TargetAssembly;
            this.RuntimeLibraryResolver = RuntimeLibraryResolver;
            this.DependencyBuilder = DependencyBuilder;
            this.Extension = Extension;
        }

        public IAssemblyBuilder TargetAssembly { get; private set; }
        public IAssemblyResolver RuntimeLibraryResolver { get; private set; }
        public IEnvironment Environment { get { return TargetAssembly.CreateBinder().Environment; } }
        public IDependencyBuilder DependencyBuilder { get; private set; }
        public string Extension { get; private set; }

        public static ICompilerOptions GetCompilerOptions(ICompilerOptions Options, IProject Project)
        {
            var projOptions = Project.GetOptions();
            var result = new StringCompilerOptions(projOptions);
            return new MergedOptions(Options, result);
        }
    }
}
