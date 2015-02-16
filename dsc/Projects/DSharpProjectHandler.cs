using Flame;
using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.DSharp.Build;
using Flame.DSharp.Lexer;
using Flame.DSharp.Parser;
using Flame.DSProject;
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
                    var dsp = sfp.ToDSProject();
                    dsp.WriteTo(Path.ChangeExtension("dsproj").Path);
                    return dsp;
                }
                else
                {
                    return sfp;
                }
            }
            else
            {
                return DSProject.ReadProject(Path.Path);
            }
        }

        public async Task<IAssembly> CompileAsync(IProject Project, CompilationParameters Parameters)
        {
            var units = await ParseCompilationUnitsAsync(Project.GetSourceItems(), Parameters.CurrentPath);
            var binder = await Parameters.BinderTask;

            var dsAsm = new SyntaxAssembly(DSharpBuildHelpers.Instance.CreatePrimitiveBinder(binder), Project.Name);
            foreach (var item in units)
            {
                dsAsm.AddCompilationUnit(item, Parameters.Log);
            }

            return dsAsm;
        }


        public static Task<CompilationUnit[]> ParseCompilationUnitsAsync(List<IProjectSourceItem> SourceItems, string CurrentPath)
        {
            Task<CompilationUnit>[] units = new Task<CompilationUnit>[SourceItems.Count];
            for (int i = 0; i < units.Length; i++)
            {
                var item = SourceItems[i];
                units[i] = ParseCompilationUnitAsync(item, CurrentPath);
            }
            return Task.WhenAll(units);
        }
        public static string GetSourceSafe(IProjectSourceItem Item, string CurrentPath)
        {
            try
            {
                return Item.GetSource(CurrentPath);
            }
            catch (FileNotFoundException ex)
            {
                ConsoleLog.Instance.LogError(new LogEntry("Error getting source code", "File '" + Item.SourceIdentifier + "' was not found"));
                return null;
            }
            catch (Exception ex)
            {
                ConsoleLog.Instance.LogError(new LogEntry("Error getting source code", "'" + Item.SourceIdentifier + "' could not be opened"));
                ConsoleLog.Instance.LogError(new LogEntry("Exception", ex.ToString()));
                return null;
            }
        }

        public static Task<CompilationUnit> ParseCompilationUnitAsync(IProjectSourceItem SourceItem, string CurrentPath)
        {
            ConsoleLog.Instance.LogEvent(new LogEntry("Status", "Parsing " + SourceItem.SourceIdentifier));
            return Task.Run(() =>
            {
                string code = GetSourceSafe(SourceItem, CurrentPath);
                if (code == null)
                {
                    return null;
                }
                var parser = new TokenizerStream(code);
                var unit = ParseCompilationUnit(parser);
                ConsoleLog.Instance.LogEvent(new LogEntry("Status", "Parsed " + SourceItem.SourceIdentifier));
                return unit;
            });
        }
        public static CompilationUnit ParseCompilationUnit(ITokenStream TokenParser)
        {
            DSharpSyntaxParser syntaxParser = new DSharpSyntaxParser(ConsoleLog.Instance);
            return syntaxParser.ParseCompilationUnit(TokenParser);
        }
    }
}
