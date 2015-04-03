using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front;
using Flame.Front.Cli;
using Flame.Front.Plugs;
using Flame.Front.Target;
using Pixie;
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
                bool hasPlatform = !string.IsNullOrWhiteSpace(BuildTargetIdentifier);
                if (hasPlatform)
                {
                    Log.LogError(new LogEntry("Unrecognized target platform", "Target platform '" + BuildTargetIdentifier + "' was not recognized as a known target platform."));
                }
                else
                {
                    Log.LogError(new LogEntry("Missing target platform", "No target platform was provided."));
                }

                var listItems = new List<IMarkupNode>();
                foreach (var item in Parser.PlatformIdentifiers)
                {
                    listItems.Add(new MarkupNode("list-item", item));
                }
                var list = new MarkupNode("list", listItems);
                string firstPlatform = Parser.PlatformIdentifiers.FirstOrDefault();
                var hint = new MarkupNode(NodeConstants.RemarksNodeType, "Prefix one of these platforms with '-platform' when providing build arguments to specify a target platform. For example: 'dsc " + Log.Options.GetOption<string>("source", CurrentPath.ToString()) + " -platform " + firstPlatform + "' will instruct the compiler to compile for the '" + firstPlatform + "' target platform.");
                var message = new MarkupNode("entry", new IMarkupNode[] { list, hint });
                Log.LogMessage(new LogEntry("Known target platforms", message));

                throw new NotSupportedException();
            }

            var rtLibs = Parser.GetRuntimeAssemblyResolver(BuildTargetIdentifier);
            var rtLibResolver = new RuntimeAssemblyResolver(rtLibs, ReferenceResolvers.ReferenceResolver, BuildTargetIdentifier);
            return parser.CreateBuildTarget(BuildTargetIdentifier, Project, Log, rtLibResolver, ReferenceResolvers.ReferenceResolver, CurrentPath, OutputDirectory);
        }
    }
}
