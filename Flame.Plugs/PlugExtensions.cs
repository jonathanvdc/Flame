using Flame.Compiler.Projects;
using Flame.RT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Plugs
{
    public static class PlugExtensions
    {
        public static PlugLibrary CreatePlugLibrary(IAssembly Assembly, IProject Project)
        {
            return new PlugLibrary(Assembly, GetNameMappingFromProject(Project));
        }

        public static PlugLibrary CreatePlugLibrary(IAssembly Assembly)
        {
            return new PlugLibrary(Assembly, GetNameMappingFromAssembly(Assembly));
        }

        public static IEnumerable<PlugAttribute> GetPlugAttributes(this IMember Member)
        {
            return Member.GetAttributes().Where((item) => item != null && item.Value != null).Select((item) => item.Value.GetObjectValue()).OfType<PlugAttribute>();
        }

        public static IReadOnlyDictionary<string, string> GetNameMappingFromProject(IProject Project)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            foreach (var item in Project.GetOptions())
            {
                if (item.Key.StartsWith("plug:"))
                {
                    results[item.Key.Substring("plug:".Length)] = item.Value;
                }
            }
            return results;
        }

        public static IReadOnlyDictionary<string, string> GetNameMappingFromAssembly(IAssembly PlugAssembly)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            foreach (var item in PlugAssembly.CreateBinder().GetTypes())
            {
                foreach (var attr in item.GetPlugAttributes())
                {
                    results[attr.PluggedName] = results[item.GetGenericFreeFullName()];
                }
            }
            return results;
        }

        public static bool IsPlugAssembly(this IAssembly Assembly, string Platform)
        {
            foreach (var item in Assembly.GetPlugAttributes())
            {
                if (item.PluggedName == Platform)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
