using Flame;
using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.DSharp.Build;
using Flame.DSharp.Lexer;
using Flame.DSharp.Parser;
using Flame.DSProject;
using Flame.Front;
using Flame.Front.Projects;
using Flame.Front.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Projects
{
    public class DSharpProjectHandler : IProjectHandler
    {
        public DSharpProjectHandler()
        {

        }

        public IEnumerable<string> Extensions
        {
            get { return new string[] { "dsproj", "ds" }; }
        }

        public IProject Parse(ProjectPath Path, ICompilerLog Log)
        {
            if (Path.HasExtension("ds"))
            {
                return new SingleFileProject(Path, Log.Options.GetTargetPlatform());
            }
            else
            {
                return DSProject.ReadProject(Path.Path.Path);
            }
        }

        public IProject MakeProject(IProject Project, ProjectPath Path, ICompilerLog Log)
        {
            var newPath = Path.Path.Parent.Combine(Project.Name).ChangeExtension("dsproj");
            var dsp = DSProject.FromProject(Project, newPath.AbsolutePath.Path);
            dsp.WriteTo(newPath.Path);
            return dsp;
        }

        public async Task<IAssembly> CompileAsync(IProject Project, CompilationParameters Parameters)
        {
            var units = await ParseCompilationUnitsAsync(Project.GetSourceItems(), Parameters);
            var binder = await Parameters.BinderTask;

            var dsAsm = new SyntaxAssembly(DSharpBuildHelpers.Instance.CreatePrimitiveBinder(binder), Parameters.Log.GetAssemblyName(Project.AssemblyName ?? Project.Name ?? ""), GetTypeNamer(Parameters.Log.Options));
            foreach (var item in units)
            {
                dsAsm.AddCompilationUnit(item, Parameters.Log);
            }

            return dsAsm;
        }

        private static IConverter<IType, string> GetTypeNamer(ICompilerOptions Options)
        {
            switch (Options.GetOption<string>("type-names", "default"))
            {
                case "trivial":
                case "prefer-trivial":
                    return new DSharpTypeNamer(true);

                case "precise":
                    return new DSharpTypeNamer(false);

                case "default":
                default:
                    return new DSharpTypeNamer();
            }
        }

        public static Task<CompilationUnit[]> ParseCompilationUnitsAsync(List<IProjectSourceItem> SourceItems, CompilationParameters Parameters)
        {
            Task<CompilationUnit>[] units = new Task<CompilationUnit>[SourceItems.Count];
            for (int i = 0; i < units.Length; i++)
            {
                var item = SourceItems[i];
                units[i] = ParseCompilationUnitAsync(item, Parameters);
            }
            return Task.WhenAll(units);
        }

        public static Task<CompilationUnit> ParseCompilationUnitAsync(IProjectSourceItem SourceItem, CompilationParameters Parameters)
        {
            Parameters.Log.LogEvent(new LogEntry("Status", "Parsing " + SourceItem.SourceIdentifier));
            return Task.Run(() =>
            {
                var code = ProjectHandlerHelpers.GetSourceSafe(SourceItem, Parameters);
                if (code == null)
                {
                    return null;
                }
                var parser = new TokenizerStream(code);
                var unit = ParseCompilationUnit(parser, Parameters.Log);
                Parameters.Log.LogEvent(new LogEntry("Status", "Parsed " + SourceItem.SourceIdentifier));
                return unit;
            });
        }
        public static CompilationUnit ParseCompilationUnit(ITokenStream TokenParser, ICompilerLog Log)
        {
            DSharpSyntaxParser syntaxParser = new DSharpSyntaxParser(Log);
            return syntaxParser.ParseCompilationUnit(TokenParser);
        }

        public IEnumerable<ParsedProject> Partition(IEnumerable<ParsedProject> Projects)
        {
            return new ParsedProject[] { new ParsedProject(Projects.First().CurrentPath, new UnionProject(Projects.Select(item => item.Project).ToArray())) };
        }
    }
}
