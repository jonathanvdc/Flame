// Based on the .NET Compiler Platform (Roslyn) MetadataHelpers.
// https://github.com/dotnet/roslyn/blob/master/src/Compilers/Core/Portable/MetadataReader/MetadataHelpers.cs

// Original license:
// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Flame.Front.AssemblyResolution
{
    public static class MetadataHelpers
    {
        public static bool IsValidPublicKey(IReadOnlyList<byte> bytes) => CryptoBlobParser.IsValidPublicKey(bytes);
    }

    internal static class Hash
    {
        /// <summary>
        /// This is how VB Anonymous Types combine hash values for fields.
        /// </summary>
        internal static int Combine(int newKey, int currentKey)
        {
            return unchecked((currentKey * (int)0xA5555529) + newKey);
        }

        internal static int Combine(bool newKeyPart, int currentKey)
        {
            return Combine(currentKey, newKeyPart ? 1 : 0);
        }
    }
}
