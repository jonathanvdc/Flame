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

namespace Flame.Front.Target
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
            Parser.RegisterParser(new FlameIRBuildTargetParser());
        }

        public static MultiBuildTargetParser Parser { get; private set; }

        public static IMarkupNode CreateTargetPlatformList()
        {
            var listItems = new List<IMarkupNode>();
            foreach (var item in Parser.PlatformIdentifiers)
            {
                listItems.Add(new MarkupNode(NodeConstants.ListItemNodeType, item));
            }
            return ListExtensions.Instance.CreateList(listItems);
        }

        public static void LogUnrecognizedTargetPlatform(ICompilerLog Log, string BuildTargetIdentifier, PathIdentifier CurrentPath)
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

            var list = CreateTargetPlatformList();
            string firstPlatform = Parser.PlatformIdentifiers.FirstOrDefault();

            var hint = new MarkupNode(NodeConstants.RemarksNodeType, 
                "Prefix one of these platforms with '-platform' when providing build arguments to specify a target platform. For example: '" + 
                (Environment.GetCommandLineArgs().FirstOrDefault() ?? "<compiler>") + " " + 
                Log.Options.GetOption<string>("source", CurrentPath.ToString()) + " -platform " + firstPlatform + 
                "' will instruct the compiler to compile for the '" + firstPlatform + "' target platform.");
            
            var message = new MarkupNode("entry", new IMarkupNode[] { list, hint });
            Log.LogMessage(new LogEntry("Known target platforms", message));
        }

        public static IBuildTargetParser GetParserOrThrow(ICompilerLog Log, string BuildTargetIdentifier, PathIdentifier CurrentPath)
        {
            var parser = Parser.GetParser(BuildTargetIdentifier);

            if (parser == null)
            {
                LogUnrecognizedTargetPlatform(Log, BuildTargetIdentifier, CurrentPath);

                throw new NotSupportedException();
            }

            return parser;
        }

        public static IDependencyBuilder CreateDependencyBuilder(IBuildTargetParser Parser, string BuildTargetIdentifier, ICompilerLog Log, PathIdentifier CurrentPath, PathIdentifier OutputDirectory)
        {
            var rtLibs = Parser.GetRuntimeAssemblyResolver(BuildTargetIdentifier, Log);
            var rtLibResolver = new RuntimeAssemblyResolver(rtLibs, ReferenceResolvers.ReferenceResolver, BuildTargetIdentifier);

            return Parser.CreateDependencyBuilder(BuildTargetIdentifier, rtLibResolver, ReferenceResolvers.ReferenceResolver, Log, CurrentPath, OutputDirectory);
        }

        public static BuildTarget CreateBuildTarget(IBuildTargetParser Parser, string BuildTargetIdentifier, IDependencyBuilder DependencyBuilder, IAssembly SourceAssembly)
        {
            var log = DependencyBuilder.Log;

            var info = new AssemblyCreationInfo(log.GetAssemblyName(SourceAssembly.Name),
                                                log.GetAssemblyVersion(new Version(1, 0, 0, 0)), 
                                                new Lazy<bool>(() => SourceAssembly.GetEntryPoint() != null));
            return Parser.CreateBuildTarget(BuildTargetIdentifier, info, DependencyBuilder);
        }
    }
}
