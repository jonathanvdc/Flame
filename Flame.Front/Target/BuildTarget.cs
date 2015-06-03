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
using Flame.Recompilation;

namespace Flame.Front.Target
{
    public class BuildTarget
    {
        public BuildTarget(IAssemblyBuilder TargetAssembly, IDependencyBuilder DependencyBuilder, string Extension, bool PreferPreserve, params string[] PreferredPasses)
            : this(TargetAssembly, DependencyBuilder, Extension, PreferPreserve, new PassPreferences(PreferredPasses))
        {
        }
        public BuildTarget(IAssemblyBuilder TargetAssembly, IDependencyBuilder DependencyBuilder, string Extension, bool PreferPreserve, PassPreferences Passes)
        {
            this.TargetAssembly = TargetAssembly;
            this.DependencyBuilder = DependencyBuilder;
            this.Extension = Extension;
            this.Passes = Passes;
            this.PreferPreserve = PreferPreserve;
        }

        public IAssemblyBuilder TargetAssembly { get; private set; }
        public IDependencyBuilder DependencyBuilder { get; private set; }        
        public string Extension { get; private set; }
        public PassPreferences Passes { get; private set; }
        public bool PreferPreserve { get; private set; }

        public static ICompilerOptions GetCompilerOptions(ICompilerOptions Options, IOptionParser<string> OptionParser, IProject Project)
        {
            var projOptions = Project.GetOptions();
            if (!string.IsNullOrWhiteSpace(Project.AssemblyName) && !projOptions.ContainsKey(LogExtensions.AssemblyNameKey))
            {
                projOptions.Add(LogExtensions.AssemblyNameKey, Project.AssemblyName);
            }
            var result = new StringCompilerOptions(projOptions, OptionParser);
            return new CachedOptions(new MergedOptions(Options, result));
        }
    }
}
