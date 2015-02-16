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
    }
}
