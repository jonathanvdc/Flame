// Based on the .NET Compiler Platform (Roslyn) ClrGlobalAssemblyCache.
// https://github.com/dotnet/roslyn/blob/8eb34a73f658fd9017f53763152570d59cbb0bde/src/Compilers/Shared/GlobalAssemblyCacheHelpers/ClrGlobalAssemblyCache.cs

// Original license:
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Flame.Front.AssemblyResolution
{
    using ASM_CACHE = GlobalAssemblyCacheLocation.ASM_CACHE;

    /// <summary>
    /// Provides APIs to enumerate and look up assemblies stored in the Global Assembly Cache.
    /// </summary>
    public sealed class ClrGlobalAssemblyCache : GlobalAssemblyCache
    {
        #region Interop

        private const int MAX_PATH = 260;

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("21b8916c-f28e-11d2-a473-00c04f8ef448")]
        private interface IAssemblyEnum
        {
            [PreserveSig]
            int GetNextAssembly(out FusionAssemblyIdentity.IApplicationContext ppAppCtx, out FusionAssemblyIdentity.IAssemblyName ppName, uint dwFlags);

            [PreserveSig]
            int Reset();

            [PreserveSig]
            int Clone(out IAssemblyEnum ppEnum);
        }

        [ComImport, Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAssemblyCache
        {
            void UninstallAssembly();

            void QueryAssemblyInfo(uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName, ref ASSEMBLY_INFO pAsmInfo);

            void CreateAssemblyCacheItem();
            void CreateAssemblyScavenger();
            void InstallAssembly();
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct ASSEMBLY_INFO
        {
            public uint cbAssemblyInfo;
            public readonly uint dwAssemblyFlags;
            public readonly ulong uliAssemblySizeInKB;
            public char* pszCurrentAssemblyPathBuf;
            public uint cchBuf;
        }

        [DllImport("clr", PreserveSig = true)]
        private static extern int CreateAssemblyEnum(out IAssemblyEnum ppEnum, FusionAssemblyIdentity.IApplicationContext pAppCtx, FusionAssemblyIdentity.IAssemblyName pName, ASM_CACHE dwFlags, IntPtr pvReserved);

        [DllImport("clr", PreserveSig = false)]
        private static extern void CreateAssemblyCache(out IAssemblyCache ppAsmCache, uint dwReserved);

        #endregion

        /// <summary>
        /// Enumerates assemblies in the GAC returning those that match given partial name and
        /// architecture.
        /// </summary>
        /// <param name="partialName">Optional partial name.</param>
        /// <param name="architectureFilter">Optional architecture filter.</param>
        public override IEnumerable<AssemblyIdentity> GetAssemblyIdentities(AssemblyName partialName, IReadOnlyList<ProcessorArchitecture> architectureFilter = null)
        {
            return GetAssemblyIdentities(FusionAssemblyIdentity.ToAssemblyNameObject(partialName), architectureFilter);
        }

        /// <summary>
        /// Enumerates assemblies in the GAC returning those that match given partial name and
        /// architecture.
        /// </summary>
        /// <param name="partialName">The optional partial name.</param>
        /// <param name="architectureFilter">The optional architecture filter.</param>
        public override IEnumerable<AssemblyIdentity> GetAssemblyIdentities(string partialName = null, IReadOnlyList<ProcessorArchitecture> architectureFilter = null)
        {
            FusionAssemblyIdentity.IAssemblyName nameObj;
            if (partialName != null)
            {
                nameObj = FusionAssemblyIdentity.ToAssemblyNameObject(partialName);
                if (nameObj == null)
                {
                    return Enumerable.Empty<AssemblyIdentity>();
                }
            }
            else
            {
                nameObj = null;
            }

            return GetAssemblyIdentities(nameObj, architectureFilter);
        }

        /// <summary>
        /// Enumerates assemblies in the GAC returning their simple names.
        /// </summary>
        /// <param name="architectureFilter">Optional architecture filter.</param>
        /// <returns>Unique simple names of GAC assemblies.</returns>
        public override IEnumerable<string> GetAssemblySimpleNames(IReadOnlyList<ProcessorArchitecture> architectureFilter = null)
        {
            var q = from nameObject in GetAssemblyObjects(partialNameFilter: null, architectureFilter: architectureFilter)
                select FusionAssemblyIdentity.GetName(nameObject);
            return q.Distinct();
        }

        private static IEnumerable<AssemblyIdentity> GetAssemblyIdentities(
            FusionAssemblyIdentity.IAssemblyName partialName,
            IReadOnlyList<ProcessorArchitecture> architectureFilter)
        {
            return from nameObject in GetAssemblyObjects(partialName, architectureFilter)
                select FusionAssemblyIdentity.ToAssemblyIdentity(nameObject);
        }

        private const int S_OK = 0;
        private const int S_FALSE = 1;

        // Internal for testing.
        internal static IEnumerable<FusionAssemblyIdentity.IAssemblyName> GetAssemblyObjects(
            FusionAssemblyIdentity.IAssemblyName partialNameFilter,
            IReadOnlyList<ProcessorArchitecture> architectureFilter)
        {
            IAssemblyEnum enumerator;
            FusionAssemblyIdentity.IApplicationContext applicationContext = null;

            int hr = CreateAssemblyEnum(out enumerator, applicationContext, partialNameFilter, ASM_CACHE.GAC, IntPtr.Zero);
            if (hr == S_FALSE)
            {
                // no assembly found
                yield break;
            }
            else if (hr != S_OK)
            {
                Exception e = Marshal.GetExceptionForHR(hr);
                if (e is FileNotFoundException)
                {
                    // invalid assembly name:
                    yield break;
                }
                else if (e != null)
                {
                    throw e;
                }
                else
                {
                    throw new ArgumentException("invalid assembly name");
                }
            }

            while (true)
            {
                FusionAssemblyIdentity.IAssemblyName nameObject;

                hr = enumerator.GetNextAssembly(out applicationContext, out nameObject, 0);
                if (hr != 0)
                {
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    break;
                }

                if (architectureFilter != null)
                {
                    var assemblyArchitecture = FusionAssemblyIdentity.GetProcessorArchitecture(nameObject);
                    if (!architectureFilter.Contains(assemblyArchitecture))
                    {
                        continue;
                    }
                }

                yield return nameObject;
            }
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

            location = null;
            FusionAssemblyIdentity.IAssemblyName nameObject = FusionAssemblyIdentity.ToAssemblyNameObject(displayName);
            if (nameObject == null)
            {
                return null;
            }

            var candidates = GetAssemblyObjects(nameObject, architectureFilter);
            string cultureName = (preferredCulture != null && !preferredCulture.IsNeutralCulture) ? preferredCulture.Name : null;

            var bestMatch = FusionAssemblyIdentity.GetBestMatch(candidates, cultureName);
            if (bestMatch == null)
            {
                return null;
            }

            location = GetAssemblyLocation(bestMatch);
            return FusionAssemblyIdentity.ToAssemblyIdentity(bestMatch);
        }

        internal static unsafe string GetAssemblyLocation(FusionAssemblyIdentity.IAssemblyName nameObject)
        {
            // NAME | VERSION | CULTURE | PUBLIC_KEY_TOKEN | RETARGET | PROCESSORARCHITECTURE
            string fullName = FusionAssemblyIdentity.GetDisplayName(nameObject, FusionAssemblyIdentity.ASM_DISPLAYF.FULL);

            fixed (char* p = new char[MAX_PATH])
            {
                ASSEMBLY_INFO info = default(ASSEMBLY_INFO);
                info = new ASSEMBLY_INFO
                {
                    cbAssemblyInfo = (uint)Marshal.SizeOf(info),
                    pszCurrentAssemblyPathBuf = p,
                    cchBuf = MAX_PATH
                };

                IAssemblyCache assemblyCacheObject;
                CreateAssemblyCache(out assemblyCacheObject, 0);
                assemblyCacheObject.QueryAssemblyInfo(0, fullName, ref info);
                Debug.Assert(info.pszCurrentAssemblyPathBuf != null);
                Debug.Assert(info.pszCurrentAssemblyPathBuf[info.cchBuf - 1] == '\0');

                var result = Marshal.PtrToStringUni((IntPtr)info.pszCurrentAssemblyPathBuf, (int)info.cchBuf - 1);
                Debug.Assert(result.IndexOf('\0') == -1);
                return result;
            }
        }
    }
}
