// Based on the .NET Compiler Platform (Roslyn) CryptographicHashProvider.
// https://github.com/dotnet/roslyn/blob/master/src/Compilers/Core/Portable/CryptographicHashProvider.cs

// Original license:
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;

namespace Flame.Front.AssemblyResolution
{
    internal static class CryptographicHashProvider
    {
        internal const int Sha1HashSize = 20;

        internal static byte[] ComputeSha1(Stream stream)
        {
            if (stream != null)
            {
                stream.Seek(0, SeekOrigin.Begin);
                using (var hashProvider = new SHA1CryptoServiceProvider())
                {
                    return hashProvider.ComputeHash(stream);
                }
            }

            return new byte[] { };
        }

        internal static byte[] ComputeSha1(IReadOnlyList<byte> bytes)
        {
            return ComputeSha1(bytes.ToArray());
        }

        internal static byte[] ComputeSha1(byte[] bytes)
        {
            using (var hashProvider = SHA1.Create())
            {
                return hashProvider.ComputeHash(bytes);
            }
        }
    }
}
