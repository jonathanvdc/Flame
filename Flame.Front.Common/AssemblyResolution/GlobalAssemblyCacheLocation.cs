// Based on the .NET Compiler Platform (Roslyn) GlobalAssemblyCacheLocation.
// https://github.com/dotnet/roslyn/blob/8eb34a73f658fd9017f53763152570d59cbb0bde/src/Compilers/Shared/GlobalAssemblyCacheHelpers/GlobalAssemblyCacheLocation.cs

// Original license:
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Flame.Front.AssemblyResolution
{
    internal static class GlobalAssemblyCacheLocation
    {
        internal enum ASM_CACHE
        {
            ZAP = 0x1,
            GAC = 0x2,                // C:\Windows\Assembly\GAC
            DOWNLOAD = 0x4,
            ROOT = 0x8,               // C:\Windows\Assembly
            GAC_MSIL = 0x10,
            GAC_32 = 0x20,            // C:\Windows\Assembly\GAC_32
            GAC_64 = 0x40,            // C:\Windows\Assembly\GAC_64
            ROOT_EX = 0x80,           // C:\Windows\Microsoft.NET\assembly
        }

        [DllImport("clr", PreserveSig = true)]
        private static unsafe extern int GetCachePath(ASM_CACHE id, byte* path, ref int length);

        public static IReadOnlyList<string> s_rootLocations;

        public static IReadOnlyList<string> RootLocations
        {
            get
            {
                if (s_rootLocations == null)
                {
                    s_rootLocations = new[] { GetLocation(ASM_CACHE.ROOT), GetLocation(ASM_CACHE.ROOT_EX) };
                }

                return s_rootLocations;
            }
        }

        private static unsafe string GetLocation(ASM_CACHE gacId)
        {
            const int ERROR_INSUFFICIENT_BUFFER = unchecked((int)0x8007007A);

            int characterCount = 0;
            int hr = GetCachePath(gacId, null, ref characterCount);
            if (hr != ERROR_INSUFFICIENT_BUFFER)
            {
                throw Marshal.GetExceptionForHR(hr);
            }

            byte[] data = new byte[(characterCount + 1) * 2];
            fixed (byte* p = data)
            {
                hr = GetCachePath(gacId, p, ref characterCount);
                if (hr != 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }

                return Marshal.PtrToStringUni((IntPtr)p);
            }
        }
    }
}
