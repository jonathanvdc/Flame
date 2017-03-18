using Flame;
using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front;
using Flame.Front.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Front.Target;
using Flame.Verification;
using Flame.Intermediate.Parsing;
using Loyc.Syntax;
using Flame.Front.Passes;

namespace Flame.Front.Projects
{
    public class FlameIRProjectHandler : IProjectHandler
    {
        public FlameIRProjectHandler()
        { }

		private static readonly Lazy<IRParser> lazyParser = new Lazy<IRParser>(() => new IRParser());
		public static IRParser Parser
		{
			get { return lazyParser.Value; }
		}

        public IEnumerable<string> Extensions
        {
            get { return new string[] { "flo", "fir" }; }
        }

        public ParsedProject Parse(ProjectPath Path, ICompilerLog Log)
        {
            var nodes = ParseFile(Path.Path);

            return new ParsedProject(
                new PathIdentifier("<file>"),
                new FlameIRProject(Path, nodes, Log));
        }

        public static IEnumerable<LNode> ParseFile(PathIdentifier Path)
        {
            if (Path.HasExtension("flo"))
            {
                using (var fs = new FileStream(Path.AbsolutePath.Path, FileMode.Open, FileAccess.Read))
                {
                    return Loyc.Binary.LoycBinaryHelpers.ReadFile(fs, Path.Name);
                }
            }
            else
            {
                using (var fs = new FileStream(Path.AbsolutePath.Path, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(fs))
                {
                    string text = reader.ReadToEnd();
                    return Loyc.Syntax.Les.LesLanguageService.Value.Parse((Loyc.UString)text, Path.Name, Loyc.MessageSink.Console);
                }
            }
        }

        public IProject MakeProject(IProject Project, ProjectPath Path, ICompilerLog Log)
        {
            Log.LogWarning(new LogEntry(
                "ignored '-make-project'",
                "the '-make-project' option was ignored because Flame IR files are self-contained assemblies: they have no use for a project."));
            return Project;
        }

        public async Task<IAssembly> CompileAsync(IProject Project, CompilationParameters Parameters)
        {
            var irProj = (FlameIRProject)Project;

            var binder = await Parameters.BinderTask;

            return Parser.ParseAssembly(binder, irProj.RootNodes);
        }

        public IEnumerable<ParsedProject> Partition(IEnumerable<ParsedProject> Projects)
        {
            return Projects;
        }

        public PassPreferences GetPassPreferences(ICompilerLog Log)
        {
            // Flame IR projects don't have any pass preferences.
            // Warnings are expected to be issued when source code
            // is compiled to the IR format, not afterwards.
            return new PassPreferences();
        }
    }
}
