using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front;
using Flame.Front.Plugs;
using Flame.Front.Target;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Target
{
    public static class BuildTargetParsers
    {
        static BuildTargetParsers()
        {
            Parser = new MultiBuildTargetParser();
            Parser.RegisterParser(new ClrBuildTargetParser());
            Parser.RegisterParser(new CppBuildTargetParser());
            Parser.RegisterParser(new PythonBuildTargetParser());
            Parser.RegisterParser(new MipsBuildTargetParser());
            Parser.RegisterParser(new ContractBuildTargetParser());
        }

        public static MultiBuildTargetParser Parser { get; private set; }

        public static BuildTarget CreateBuildTarget(IProject Project, ICompilerLog Log, string BuildTargetIdentifier, PathIdentifier CurrentPath, PathIdentifier OutputDirectory)
        {
            var parser = Parser.GetParser(BuildTargetIdentifier);

            if (parser == null)
            {
                StringBuilder message = new StringBuilder();
                if (string.IsNullOrWhiteSpace(BuildTargetIdentifier))
                {
                    message.AppendLine("No target platform was provided.");
                    message.AppendLine("To specify a platform, use the -platform option with one of the following:");
                }
                else
                {
                    message.Append("Target platform '").Append(BuildTargetIdentifier).AppendLine("' was not recognized as a known target platform.");
                    message.AppendLine("Known target platforms:");
                }
                foreach (var item in Parser.PlatformIdentifiers)
                {
                    message.Append(" * ").AppendLine(item);
                }
                Log.LogError(new LogEntry("Unrecognized target platform", message.ToString()));
                throw new NotSupportedException();
            }

            var rtLibs = Parser.GetRuntimeAssemblyResolver(BuildTargetIdentifier);
            var rtLibResolver = new RuntimeAssemblyResolver(rtLibs, ReferenceResolvers.ReferenceResolver, BuildTargetIdentifier);
            return parser.CreateBuildTarget(BuildTargetIdentifier, Project, Log, rtLibResolver, ReferenceResolvers.ReferenceResolver, CurrentPath, OutputDirectory);
        }
    }
}
