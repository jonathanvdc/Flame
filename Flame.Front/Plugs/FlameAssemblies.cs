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
            flameAssemblyNameMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            MapFlameDll(FlameRTName);
            MapFlameDll(FlameName);
            MapFlameDll(FlameCompilerName);
            MapFlameDll(FlameSyntaxName);
            MapFlameDll(FlameDSharpName);
        }

        public const string FlameRTName = "Flame.RT";
        public const string FlameName = "Flame";
        public const string FlameCompilerName = "Flame.Compiler";
        public const string FlameSyntaxName = "Flame.Syntax";
        public const string FlameDSharpName = "Flame.DSharp";

        private static PathIdentifier flameAsmBasePath;
        public static PathIdentifier FlameAssemblyBasePath
        {
            get
            {
                if (flameAsmBasePath == null)
                {
                    flameAsmBasePath = new PathIdentifier(Assembly.GetExecutingAssembly().Location);
                }
                return flameAsmBasePath;
            }
        }

        public static PathIdentifier FlameAssemblyDirectory
        {
            get
            {
                return FlameAssemblyBasePath.Parent;
            }
        }

        private static Dictionary<string, string> flameAssemblyNameMapping;
        public static void MapFlameAssembly(string Name, string FileName)
        {
            flameAssemblyNameMapping[Name] = FileName;
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
            return flameAssemblyNameMapping.ContainsKey(Name);
        }

        public static PathIdentifier GetFlameAssemblyPath(string Name)
        {
            return new PathIdentifier(flameAssemblyNameMapping[Name]);
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
