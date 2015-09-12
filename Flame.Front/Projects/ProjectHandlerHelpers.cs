using Flame.Compiler;
using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    public static class ProjectHandlerHelpers
    {
        public static ISourceDocument GetSourceSafe(IProjectSourceItem Item, CompilationParameters Parameters)
        {
            try
            {
                return Item.GetSource(Parameters.CurrentPath.AbsolutePath.Path);
            }
            catch (FileNotFoundException)
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
    }
}
