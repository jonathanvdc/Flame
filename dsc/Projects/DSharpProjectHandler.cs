using Flame;
using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.DSharp.Build;
using Flame.DSharp.Lexer;
using Flame.DSharp.Parser;
using Flame.DSProject;
using Flame.Front;
using Flame.Front.Projects;
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

        public IProject Parse(ProjectPath Path)
        {
            if (Path.HasExtension("ds"))
            {
                var sfp = new SingleFileProject(Path);
                if (Path.MakeProject)
                {
                    var dsp = DSProject.FromProject(sfp, sfp.FilePath.AbsolutePath.Path);
                    dsp.WriteTo(Path.ChangeExtension("dsproj").Path.Path);
                    return dsp;
                }
                else
                {
                    return sfp;
                }
            }
            else
            {
                return DSProject.ReadProject(Path.Path.Path);
            }
        }

        public async Task<IAssembly> CompileAsync(IProject Project, CompilationParameters Parameters)
        {
            var units = await ParseCompilationUnitsAsync(Project.GetSourceItems(), Parameters);
            var binder = await Parameters.BinderTask;

            var dsAsm = new SyntaxAssembly(DSharpBuildHelpers.Instance.CreatePrimitiveBinder(binder), Project.Name);
            foreach (var item in units)
            {
                dsAsm.AddCompilationUnit(item, Parameters.Log);
            }

            return dsAsm;
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
        public static ISourceDocument GetSourceSafe(IProjectSourceItem Item, CompilationParameters Parameters)
        {
            try
            {
                return Item.GetSource(Parameters.CurrentPath.AbsolutePath.Path);
            }
            catch (FileNotFoundException ex)
            {
                Parameters.Log.LogError(new LogEntry("Error getting source code", "File '" + Item.SourceIdentifier + "' was not found"));
                return null;
            }
            catch (Exception ex)
            {
                Parameters.Log.LogError(new LogEntry("Error getting source code", "'" + Item.SourceIdentifier + "' could not be opened"));
                Parameters.Log.LogError(new LogEntry("Exception", ex.ToString()));
                return null;
            }
        }

        public static Task<CompilationUnit> ParseCompilationUnitAsync(IProjectSourceItem SourceItem, CompilationParameters Parameters)
        {
            Parameters.Log.LogEvent(new LogEntry("Status", "Parsing " + SourceItem.SourceIdentifier));
            return Task.Run(() =>
            {
                var code = GetSourceSafe(SourceItem, Parameters);
                if (code == null)
                {
                    return null;
                }
                var parser = new TokenizerStream(code);
                var unit = ParseCompilationUnit(parser);
                Parameters.Log.LogEvent(new LogEntry("Status", "Parsed " + SourceItem.SourceIdentifier));
                return unit;
            });
        }
        public static CompilationUnit ParseCompilationUnit(ITokenStream TokenParser)
        {
            DSharpSyntaxParser syntaxParser = new DSharpSyntaxParser(Program.CompilerLog);
            return syntaxParser.ParseCompilationUnit(TokenParser);
        }
    }
}
