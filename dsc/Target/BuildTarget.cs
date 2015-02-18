using dsc.Options;
using dsc.Plugs;
using Flame;
using Flame.Cecil;
using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Cpp;
using Flame.Python;
using Flame.TextContract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Target
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

        public static IAssemblyResolver GetRuntimeAssemblyResolver(string BuildTargetIdentifier)
        {
            switch (BuildTargetIdentifier.Split('\\', '/')[0].ToLower())
            {
                case "clr":
                    return CecilRuntimeLibraries.Resolver;
                case "python":
                case "contract":
                case "c++":
                    return new EmptyAssemblyResolver();
                case "mars":
                    return MarsRuntimeLibraries.Resolver;
                default:
                    throw new NotSupportedException();
            }
        }

        public static BuildTarget CreateBuildTarget(IProject Project, ICompilerLog Log, string BuildTargetIdentifier, string CurrentPath, string OutputDirectory)
        {
            string lowerBuildTarget = BuildTargetIdentifier.ToLower();
            var rtLibResolver = new RuntimeAssemblyResolver(GetRuntimeAssemblyResolver(BuildTargetIdentifier), BuildTargetIdentifier);
            if (lowerBuildTarget == "clr" || lowerBuildTarget.StartsWith("clr/"))
            {
                var resolver = new Flame.Cecil.SpecificAssemblyResolver();
                Mono.Cecil.ModuleKind moduleKind;
                string extension;
                switch (lowerBuildTarget.Substring(3))
                {
                    case "/release-console":
                        moduleKind = Mono.Cecil.ModuleKind.Console;
                        extension = "exe";
                        break;
                    case "/release":
                    default:
                        moduleKind = Mono.Cecil.ModuleKind.Dll;
                        extension = "dll";
                        break;
                }
                var asm = new CecilAssembly(Project.AssemblyName, new Version(), moduleKind, resolver, Log);
                var cecilDepBuilder = new DependencyBuilder(rtLibResolver, asm.CreateBinder().Environment, CurrentPath, OutputDirectory);
                cecilDepBuilder.SetCecilResolver(resolver);
                return new BuildTarget(asm, rtLibResolver, cecilDepBuilder, extension);
            }
            IAssemblyBuilder targetAsm;
            string targetExt;
            switch (lowerBuildTarget)
            {
                case "python":
                    targetAsm = new PythonAssembly(Project.AssemblyName, new Version(), new PythonifyingMemberNamer());
                    targetExt = "py";
                    break;
                case "contract":
                    targetAsm = new ContractAssembly(Project.AssemblyName, new ContractEnvironment());
                    targetExt = "txt";
                    break;
                case "c++":
                    targetAsm = new CppAssembly(Project.AssemblyName, new Version());
                    targetExt = "cpp";
                    break;
                case "mips":
                    targetAsm = new Flame.MIPS.AssemblerAssembly(Project.AssemblyName, new Version(), new Flame.MIPS.MarsEnvironment());
                    targetExt = "asm";
                    break;
                default:
                    throw new NotSupportedException();
            }
            var depBuilder = new DependencyBuilder(rtLibResolver, targetAsm.CreateBinder().Environment, CurrentPath, OutputDirectory);
            return new BuildTarget(targetAsm, rtLibResolver, depBuilder, targetExt);
        }

        public static ICompilerOptions GetCompilerOptions(IDictionary<string, string> Options, IProject Project)
        {
            var projOptions = Project.GetOptions();
            var result = new StringCompilerOptions(projOptions);
            foreach (var item in Options)
            {
                result[item.Key] = item.Value;
            }
            return result;
        }
    }
}
