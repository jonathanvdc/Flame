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
                MarkupNode doc;

                if (string.IsNullOrWhiteSpace(Path.Extension))
                {
                    doc = new MarkupNode(NodeConstants.TextNodeType,
                        "'" + Path.Path.Path + "' does not have an extension.");
                }
                else
                {
                    doc = new MarkupNode(NodeConstants.TextNodeType,
                        "Extension '" + Path.Extension + "' in '" + Path.Path.Path + "' was not recognized as a known project extension.");
                }
                var listItems = new List<IMarkupNode>();
                foreach (var item in handlers.SelectMany(item => item.Extensions))
                {
                    listItems.Add(new MarkupNode(NodeConstants.ListItemNodeType, item));
                }
                var listHeader = new MarkupNode(NodeConstants.BrightNodeType, new IMarkupNode[] 
                { 
                    new MarkupNode(NodeConstants.TextNodeType, "Supported extensions:") 
                });
                var list = ListExtensions.Instance.CreateList(listHeader, listItems);
                // Log.LogMessage(new LogEntry("Supported extensions", list));
                var body = new MarkupNode("entry", new IMarkupNode[] { doc, list });
                throw new AbortCompilationException(new LogEntry("Invalid extension", body));
            }
            return handler;
        }
    }
}
