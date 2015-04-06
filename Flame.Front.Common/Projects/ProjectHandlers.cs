using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front;
using Flame.Front.Cli;
using Flame.Front.Projects;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    public static class ProjectHandlers
    {
        static ProjectHandlers()
        {
            handlers = new List<IProjectHandler>();
        }

        private static List<IProjectHandler> handlers;

        public static void RegisterHandler(IProjectHandler Handler)
        {
            handlers.Add(Handler);
        }
        public static IProjectHandler GetHandler(ProjectPath Path)
        {
            foreach (var item in handlers)
            {
                foreach (var ext in item.Extensions)
                {
                    if (Path.HasExtension(ext))
                    {
                        return item;
                    }
                }
            }
            return null;
        }

        public static IProjectHandler GetProjectHandler(ProjectPath Path, ICompilerLog Log)
        {
            var handler = ProjectHandlers.GetHandler(Path);
            if (handler == null)
            {
                if (string.IsNullOrWhiteSpace(Path.Extension))
                {
                    Log.LogError(new LogEntry("Invalid extension", "'" + Path.Path.Path + "' does not have an extension."));
                }
                else
                {
                    Log.LogError(new LogEntry("Invalid extension", "Extension '" + Path.Extension + "' in '" + Path.Path.Path + "' was not recognized as a known project extension."));
                }
                var listItems = new List<IMarkupNode>();
                foreach (var item in handlers.SelectMany(item => item.Extensions))
                {
                    listItems.Add(new MarkupNode(NodeConstants.ListItemNodeType, item));
                }
                var list = ListExtensions.Instance.CreateList(listItems);
                Log.LogMessage(new LogEntry("Supported extensions", list));
                throw new NotSupportedException();
            }
            return handler;
        }
    }
}
