// Based on the .NET Compiler Platform (Roslyn) GlobalAssemblyCache.
// https://github.com/dotnet/roslyn/blob/8eb34a73f658fd9017f53763152570d59cbb0bde/src/Compilers/Shared/GlobalAssemblyCacheHelpers/GlobalAssemblyCache.cs

// Original license:
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Flame.Front.AssemblyResolution
{
    /// <summary>
    /// Provides APIs to enumerate and look up assemblies stored in the Global Assembly Cache.
    /// </summary>
    public abstract class GlobalAssemblyCache
    {
        public static readonly GlobalAssemblyCache Instance = CreateInstance();

        private static GlobalAssemblyCache CreateInstance()
        {
            if (Type.GetType("Mono.Runtime") != null)
            {
                return new MonoGlobalAssemblyCache();
            }
            else
            {
                return new ClrGlobalAssemblyCache();
            }
        }

        /// <summary>
        /// Represents the current Processor architecture.
        /// </summary>
        public static readonly IReadOnlyList<ProcessorArchitecture> CurrentArchitectures = (IntPtr.Size == 4)
            ? new[] { ProcessorArchitecture.None, ProcessorArchitecture.MSIL, ProcessorArchitecture.X86 }
            : new[] { ProcessorArchitecture.None, ProcessorArchitecture.MSIL, ProcessorArchitecture.Amd64 };

        /// <summary>
        /// Enumerates assemblies in the GAC returning those that match given partial name and
        /// architecture.
        /// </summary>
        /// <param name="partialName">Optional partial name.</param>
        /// <param name="architectureFilter">Optional architecture filter.</param>
        public abstract IEnumerable<AssemblyIdentity> GetAssemblyIdentities(AssemblyName partialName, IReadOnlyList<ProcessorArchitecture> architectureFilter = null);

        /// <summary>
        /// Enumerates assemblies in the GAC returning those that match given partial name and
        /// architecture.
        /// </summary>
        /// <param name="partialName">The optional partial name.</param>
        /// <param name="architectureFilter">The optional architecture filter.</param>
        public abstract IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string partialName = null, IReadOnlyList<ProcessorArchitecture> architectureFilter = null);

        /// <summary>
        /// Enumerates assemblies in the GAC returning their simple names.
        /// </summary>
        /// <param name="architectureFilter">Optional architecture filter.</param>
        /// <returns>Unique simple names of GAC assemblies.</returns>
        public abstract IEnumerable<string> GetAssemblySimpleNames(IReadOnlyList<ProcessorArchitecture> architectureFilter = null);

        /// <summary>
        /// Looks up specified partial assembly name in the GAC and returns the best matching <see cref="AssemblyIdentity"/>.
        /// </summary>
        /// <param name="displayName">The display name of an assembly</param>
        /// <param name="architectureFilter">The optional processor architecture</param>
        /// <param name="preferredCulture">The optional preferred culture information</param>
        /// <returns>An assembly identity or null, if <paramref name="displayName"/> can't be resolved.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="displayName"/> is null.</exception>
        public AssemblyIdentity ResolvePartialName(
            string displayName,
            IReadOnlyList<ProcessorArchitecture> architectureFilter = null,
            CultureInfo preferredCulture = null)
        {
            string location;
            return ResolvePartialName(displayName, out location, architectureFilter, preferredCulture);
        }

        /// <summary>
        /// Looks up specified partial assembly name in the GAC and returns the best matching <see cref="AssemblyIdentity"/>.
        /// </summary>
        /// <param name="displayName">The display name of an assembly</param>
        /// <param name="location">Full path name of the resolved assembly</param>
        /// <param name="architectureFilter">The optional processor architecture</param>
        /// <param name="preferredCulture">The optional preferred culture information</param>
        /// <returns>An assembly identity or null, if <paramref name="displayName"/> can't be resolved.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="displayName"/> is null.</exception>
        public abstract AssemblyIdentity ResolvePartialName(
            string displayName,
            out string location,
            IReadOnlyList<ProcessorArchitecture> architectureFilter = null,
            CultureInfo preferredCulture = null);
    }
}
