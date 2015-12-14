using Flame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Plugs
{
    public static class PlugHandler
    {
        public static IReadOnlyDictionary<string, IEnumerable<string>> PlugAlternatives
        {
            get
            {
                return new Dictionary<string, IEnumerable<string>>()
                {
                    { "PortableRT", new[] { "PlatformRT" } }
                };
            }
        }

        public static string ToValidPath(string TargetPlatform)
        {
            StringBuilder sb = new StringBuilder(TargetPlatform);
            Dictionary<char, char> invalidCharDict = new Dictionary<char, char>()
            {
                { '+', 'p' },
                { '-', 'm' },
                { '#', 's' },
                { '*', 's' },
                { '.', 'd' }
            };
            foreach (char item in Path.GetInvalidPathChars())
            {
                if (invalidCharDict.ContainsKey(item))
                {
                    sb.Replace(item, invalidCharDict[item]);
                }
                else
                {
                    sb.Replace(item.ToString(), "");
                }
            }
            return sb.ToString();
        }

        private static string plugDir;
        public static string PlugDirectory
        {
            get
            {
                if (plugDir == null)
                {
                    string curDir = Assembly.GetExecutingAssembly().Location;
                    plugDir = Path.Combine(Path.GetDirectoryName(curDir), "Plugs");
                    if (!Directory.Exists(plugDir))
                    {
                        Directory.CreateDirectory(plugDir);
                    }
                }
                return plugDir;
            }
        }

        public static string GetPlugDirectoryPath(string TargetPlatform)
        {
            string path = Path.Combine(PlugDirectory, ToValidPath(TargetPlatform));
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }

        public static IEnumerable<Task<IAssembly>> GetPlugAssemblyTasks(IAssemblyResolver Resolver, string TargetPlatform)
        {
            foreach (var item in Directory.GetFiles(GetPlugDirectoryPath(TargetPlatform)))
            {
                yield return Resolver.ResolveAsync(new PathIdentifier(item), new PathIdentifier(PlugDirectory), null);
            }
        }

        public static async Task<IAssembly> GetPlugAssemblyAsync(IAssemblyResolver Resolver, string TargetPlatform, string AssemblyName)
        {
            string platform = TargetPlatform.Split('\\', '/').First();
            List<IAssembly> asms = new List<IAssembly>();
            foreach (var item in GetPlugAssemblyTasks(Resolver, platform))
            {
                var asm = await item;
                if (asm.Name == AssemblyName)
                {
                    return asm;
                }
                asms.Add(asm);
            }
            var alts = PlugAlternatives;
            if (alts.ContainsKey(AssemblyName))
            {
                foreach (var item in alts[AssemblyName])
                {
                    foreach (var asm in asms)
                    {
                        if (asm.Name == item)
                        {
                            return asm;
                        }
                    }
                }
            }
            return null;
        }
    }
}
