using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front;
using Flame.Front.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Projects
{
    public static class ProjectHandlers
    {
        static ProjectHandlers()
        {
            handlers = new List<IProjectHandler>();
            RegisterHandler(new DSharpProjectHandler());
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

        public static IProjectHandler GetProjectHandler(ProjectPath Path)
        {
            var handler = ProjectHandlers.GetHandler(Path);
            if (handler == null)
            {
                if (string.IsNullOrWhiteSpace(Path.Extension))
                {
                    ConsoleLog.Instance.LogError(new LogEntry("Invalid extension", "'" + Path.Path.Path + "' does not have an extension."));
                }
                else
                {
                    ConsoleLog.Instance.LogError(new LogEntry("Invalid extension", "Extension '" + Path.Extension + "' in '" + Path.Path.Path + "' was not recognized as a known project extension."));
                }
                ConsoleLog.Instance.WriteLine("Supported extensions:");
                foreach (var item in handlers.SelectMany(item => item.Extensions))
                {
                    ConsoleLog.Instance.WriteLine(" *." + item, ConsoleColor.Yellow);
                }
                throw new NotSupportedException();
            }
            return handler;
        }
    }
}
