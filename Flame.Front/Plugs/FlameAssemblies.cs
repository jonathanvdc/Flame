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
    public static class FlameAssemblies
    {
        static FlameAssemblies()
        {
            FlameAssemblyBasePath = new PathIdentifier(Assembly.GetExecutingAssembly().Location);
        }

        private static void InitializeFlameAssemblies()
        {
            flameAssemblyNameMappingDict = new Dictionary<string, PathIdentifier>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in Directory.GetFiles(FlameAssemblyDirectory.Path, "*.dll", SearchOption.TopDirectoryOnly))
            {
                var ident = new PathIdentifier(item);
                MapFlameAssembly(ident.NameWithoutExtension, ident.Name);
            }
        }

        public const string PixieName = "Pixie";
        public const string FlameRTName = "Flame.RT";
        public const string FlameName = "Flame";
        public const string FlameCompilerName = "Flame.Compiler";
        public const string FlameSyntaxName = "Flame.Syntax";
        public const string FlameDSharpName = "Flame.DSharp";

        public static PathIdentifier FlameAssemblyBasePath { get; private set; }

        public static PathIdentifier FlameAssemblyDirectory
        {
            get
            {
                return FlameAssemblyBasePath.Parent;
            }
        }

        private static Dictionary<string, PathIdentifier> flameAssemblyNameMappingDict;
        private static Dictionary<string, PathIdentifier> FlameAssemblyNameMapping
        {
            get
            {
                if (flameAssemblyNameMappingDict == null)
                {
                    InitializeFlameAssemblies();
                }
                return flameAssemblyNameMappingDict;
            }
        }

        public static IEnumerable<PathIdentifier> FlameAssemblyPaths
        {
            get { return FlameAssemblyNameMapping.Values; }
        }
        
        public static void MapFlameAssembly(string Name, string FileName)
        {
            FlameAssemblyNameMapping[Name] = new PathIdentifier(FlameAssemblyDirectory, FileName);
        }
        public static void MapFlameAssemblyByExtension(string Name, string Extension)
        {
            MapFlameAssembly(Name, Name + "." + Extension.TrimStart('.'));
        }
        public static void MapFlameDll(string Name)
        {
            MapFlameAssemblyByExtension(Name, "dll");
        }

        public static bool IsFlameAssembly(string Name)
        {
            return FlameAssemblyNameMapping.ContainsKey(Name);
        }

        public static PathIdentifier GetFlameAssemblyPath(string Name)
        {
            return FlameAssemblyNameMapping[Name];
        }

        public static Task<IAssembly> GetFlameAssemblyAsync(IAssemblyResolver Resolver, string Name)
        {
            var asmPath = GetFlameAssemblyPath(Name);
            return Resolver.ResolveAsync(asmPath, null);
        }

        public static Task<IAssembly> GetFlameRTAsync(IAssemblyResolver Resolver)
        {
            return GetFlameAssemblyAsync(Resolver, FlameRTName);
        }
    }
}
