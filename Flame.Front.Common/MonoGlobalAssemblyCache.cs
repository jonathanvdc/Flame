// Based on the .NET Compiler Platform (Roslyn) MonoGlobalAssemblyCache.
// https://github.com/dotnet/roslyn/blob/8eb34a73f658fd9017f53763152570d59cbb0bde/src/Compilers/Shared/GlobalAssemblyCacheHelpers/MonoGlobalAssemblyCache.cs

// Original license:
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Flame.Front
{
    /// <summary>
    /// Provides APIs to enumerate and look up assemblies stored in the Global Assembly Cache.
    /// </summary>
    public static class MonoGlobalAssemblyCache
    {
        public static readonly Lazy<IEnumerable<string>> RootLocations = new Lazy<IEnumerable<string>>(() => new string[] { GetMonoCachePath() });

        private static string GetAssemblyLocation(Assembly assembly)
        {
            return assembly.Location;
        }

        private static string GetMonoCachePath()
        {
            string file = GetAssemblyLocation(typeof(Uri).GetTypeInfo().Assembly);
            return Directory.GetParent(Path.GetDirectoryName(file)).Parent.FullName;
        }

        private static IEnumerable<string> GetCorlibPaths(Version version)
        {
            string corlibPath = GetAssemblyLocation(typeof(object).GetTypeInfo().Assembly);
            var corlibParentDir = Directory.GetParent(corlibPath).Parent;

            var corlibPaths = new List<string>();

            foreach (var corlibDir in corlibParentDir.GetDirectories())
            {
                var path = Path.Combine(corlibDir.FullName, "mscorlib.dll");
                if (!File.Exists(path))
                {
                    continue;
                }

                var name = new AssemblyName(path);
                if (version != null && name.Version != version)
                {
                    continue;
                }

                corlibPaths.Add(path);
            }

            return corlibPaths;
        }

        private static IEnumerable<string> GetGacAssemblyPaths(string gacPath, string name, Version version, string publicKeyToken)
        {
            if (version != null && publicKeyToken != null)
            {
                yield return Path.Combine(gacPath, name, version + "__" + publicKeyToken, name + ".dll");
                yield break;
            }

            var gacAssemblyRootDir = new DirectoryInfo(Path.Combine(gacPath, name));
            if (!gacAssemblyRootDir.Exists)
            {
                yield break;
            }

            foreach (var assemblyDir in gacAssemblyRootDir.GetDirectories())
            {
                if (version != null && !assemblyDir.Name.StartsWith(version.ToString(), StringComparison.Ordinal))
                {
                    continue;
                }

                if (publicKeyToken != null && !assemblyDir.Name.EndsWith(publicKeyToken, StringComparison.Ordinal))
                {
                    continue;
                }

                var assemblyPath = Path.Combine(assemblyDir.ToString(), name + ".dll");
                if (File.Exists(assemblyPath))
                {
                    yield return assemblyPath;
                }
            }
        }

        private static IEnumerable<string> GetAssemblyPaths(AssemblyName name)
        {
            if (name == null)
            {
                return GetAssemblyPaths(null, null, null);
            }

            string publicKeyToken = null;
            if (name.GetPublicKeyToken() != null)
            {
                var sb = new StringBuilder();
                foreach (var b in name.GetPublicKeyToken())
                {
                    sb.AppendFormat("{0:x2}", b);
                }

                publicKeyToken = sb.ToString();
            }

            return GetAssemblyPaths(name.Name, name.Version, publicKeyToken);
        }

        private static IEnumerable<string> GetAssemblyPaths(string name, Version version, string publicKeyToken)
        {
            foreach (string gacPath in RootLocations.Value)
            {
                var assemblyPaths = (name == "mscorlib") ?
                    GetCorlibPaths(version) :
                    GetGacAssemblyPaths(gacPath, name, version, publicKeyToken);

                foreach (var assemblyPath in assemblyPaths)
                {
                    if (!File.Exists(assemblyPath))
                    {
                        continue;
                    }

                    var gacAssemblyName = new AssemblyName(assemblyPath);

                    yield return assemblyPath;
                }
            }
        }

        public static string ResolvePartialName(string displayName)
        {
            if (displayName == null)
            {
                throw new ArgumentNullException("displayName");
            }

            var assemblyName = new AssemblyName(displayName);

            foreach (var assemblyPath in GetAssemblyPaths(assemblyName))
            {
                if (!File.Exists(assemblyPath))
                {
                    continue;
                }

                return assemblyPath;
            }

            return null;
        }
    }
}