﻿using Flame.Front.Target;
using Flame;
using Flame.Cecil;
using Flame.Compiler;
using Flame.Front;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public static class CecilRuntimeLibraries
    {
        static CecilRuntimeLibraries()
        {
            resolver = new CecilRTLibraryResolver();
        }

        private static IAssemblyResolver resolver;
        public static IAssemblyResolver Resolver
        {
            get { return resolver; }
        }

        public static IAssembly RevolveRuntimeLibrary(string Identifier, IDependencyBuilder DependencyBuilder)
        {
            Assembly loadedAsm;
            switch (Identifier)
            {
                case "PlatformRT":
                case "PortableRT":
                case "mscorlib":
                    loadedAsm = typeof(Math).Assembly;
                    break;
                case "System":
                    loadedAsm = typeof(System.Net.WebRequest).Assembly;
                    break;
                case "System.Core":
                    loadedAsm = typeof(Enumerable).Assembly;
                    break;
                case "System.Xml":
                    loadedAsm = typeof(System.Xml.XmlDocument).Assembly;
                    break;
                default:
                    return null;
            }
            var asmDef = Mono.Cecil.AssemblyDefinition.ReadAssembly(loadedAsm.Location, new Mono.Cecil.ReaderParameters() 
            {
                AssemblyResolver = DependencyBuilder.GetCecilResolver() 
            });
            return new CecilAssembly(asmDef, CecilReferenceResolver.ConversionCache);
        }

        private class CecilRTLibraryResolver : IAssemblyResolver
        {
            public async Task<IAssembly> ResolveAsync(PathIdentifier Identifier, IDependencyBuilder DependencyBuilder)
            {
                return RevolveRuntimeLibrary(Identifier.Path, DependencyBuilder);
            }

            public async Task<PathIdentifier?> CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier, ICompilerLog Log)
            {
                return null;
            }
        }
    }
}
