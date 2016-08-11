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

namespace Flame.Front.AssemblyResolution
{
    /// <summary>
    /// Provides APIs to enumerate and look up assemblies stored in the Global Assembly Cache.
    /// </summary>
    public sealed class MonoGlobalAssemblyCache : GlobalAssemblyCache
    {
        public static readonly IReadOnlyList<string> RootLocations;

        static MonoGlobalAssemblyCache()
        {
            RootLocations = new string[] { GetMonoCachePath() };
        }

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

        private static IEnumerable<Tuple<AssemblyIdentity, string>> GetAssemblyIdentitiesAndPaths(AssemblyName name, IReadOnlyList<ProcessorArchitecture> architectureFilter)
        {
            if (name == null)
            {
                return GetAssemblyIdentitiesAndPaths(null, null, null, architectureFilter);
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

            return GetAssemblyIdentitiesAndPaths(name.Name, name.Version, publicKeyToken, architectureFilter);
        }

        private static IEnumerable<Tuple<AssemblyIdentity, string>> GetAssemblyIdentitiesAndPaths(string name, Version version, string publicKeyToken, IReadOnlyList<ProcessorArchitecture> architectureFilter)
        {
            foreach (string gacPath in RootLocations)
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

                    if (gacAssemblyName.ProcessorArchitecture != ProcessorArchitecture.None &&
                        architectureFilter != null &&
                        architectureFilter.Count > 0 &&
                        !architectureFilter.Contains(gacAssemblyName.ProcessorArchitecture))
                    {
                        continue;
                    }

                    var assemblyIdentity = new AssemblyIdentity(
                        gacAssemblyName.Name,
                        gacAssemblyName.Version,
                        gacAssemblyName.CultureName,
                        gacAssemblyName.GetPublicKeyToken());

                    yield return new Tuple<AssemblyIdentity, string>(assemblyIdentity, assemblyPath);
                }
            }
        }

        public override IEnumerable<AssemblyIdentity> GetAssemblyIdentities(AssemblyName partialName, IReadOnlyList<ProcessorArchitecture> architectureFilter = null)
        {
            return GetAssemblyIdentitiesAndPaths(partialName, architectureFilter).Select(identityAndPath => identityAndPath.Item1);
        }

        public override IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string partialName = null, IReadOnlyList<ProcessorArchitecture> architectureFilter = null)
        {
            AssemblyName name;
            try
            {
                name = (partialName == null) ? null : new AssemblyName(partialName);
            }
            catch
            {
                return Enumerable.Empty<AssemblyIdentity>();
            }

            return GetAssemblyIdentities(name, architectureFilter);
        }

        public override IEnumerable<string> GetAssemblySimpleNames(IReadOnlyList<ProcessorArchitecture> architectureFilter = null)
        {
            return GetAssemblyIdentitiesAndPaths(name: null, version: null, publicKeyToken: null, architectureFilter: architectureFilter).
                Select(identityAndPath => identityAndPath.Item1.Name).Distinct();
        }

        public override AssemblyIdentity ResolvePartialName(
            string displayName,
            out string location,
            IReadOnlyList<ProcessorArchitecture> architectureFilter,
            CultureInfo preferredCulture)
        {
            if (displayName == null)
            {
                throw new ArgumentNullException("displayName");
            }

            string cultureName = (preferredCulture != null && !preferredCulture.IsNeutralCulture) ? preferredCulture.Name : null;

            var assemblyName = new AssemblyName(displayName);
            AssemblyIdentity assemblyIdentity = null;

            location = null;
            bool isBestMatch = false;

            foreach (var identityAndPath in GetAssemblyIdentitiesAndPaths(assemblyName, architectureFilter))
            {
                var assemblyPath = identityAndPath.Item2;

                if (!File.Exists(assemblyPath))
                {
                    continue;
                }

                var gacAssemblyName = new AssemblyName(assemblyPath);

                isBestMatch = cultureName == null || gacAssemblyName.CultureName == cultureName;
                bool isBetterMatch = location == null || isBestMatch;

                if (isBetterMatch)
                {
                    location = assemblyPath;
                    assemblyIdentity = identityAndPath.Item1;
                }

                if (isBestMatch)
                {
                    break;
                }
            }

            return assemblyIdentity;
        }
    }
}