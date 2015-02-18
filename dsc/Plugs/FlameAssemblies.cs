using Flame;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Plugs
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

        private static Uri flameAsmBasePath;
        public static Uri FlameAssemblyBasePath
        {
            get
            {
                if (flameAsmBasePath == null)
                {
                    flameAsmBasePath = new Uri(Assembly.GetExecutingAssembly().Location);
                }
                return flameAsmBasePath;
            }
        }

        public static string FlameAssemblyDirectory
        {
            get
            {
                return Path.GetDirectoryName(FlameAssemblyBasePath.AbsolutePath);
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

        public static string GetFlameAssemblyPath(string Name)
        {
            var uri = new Uri(flameAssemblyNameMapping[Name], UriKind.RelativeOrAbsolute);
            return new Uri(FlameAssemblyBasePath, uri).AbsolutePath;
        }

        public static Task<IAssembly> GetFlameAssemblyAsync(string Name)
        {
            string asmPath = GetFlameAssemblyPath(Name);
            return ReferenceResolvers.ResolveAsync(null, asmPath, asmPath);
        }

        public static Task<IAssembly> GetFlameRTAsync()
        {
            return GetFlameAssemblyAsync(FlameRTName);
        }
    }
}
