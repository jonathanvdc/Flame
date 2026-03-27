using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Flame.TypeSystem;

namespace Flame.Clr
{
    /// <summary>
    /// An assembly resolver implementation that forwards assembly
    /// resolution requests to Mono.Cecil assembly resolver.
    /// </summary>
    public sealed class CecilAssemblyResolver : AssemblyResolver
    {
        /// <summary>
        /// Creates a Flame assembly resolver that forwards requests
        /// to a Mono.Cecil assembly resolver.
        /// </summary>
        /// <param name="resolver">The Mono.Cecil assembly resolver to use.</param>
        /// <param name="typeEnvironment">The Flame type environment to use when analyzing assemblies.</param>
        public CecilAssemblyResolver(
            Mono.Cecil.IAssemblyResolver resolver,
            TypeEnvironment typeEnvironment)
            : this(resolver, typeEnvironment, new Mono.Cecil.ReaderParameters())
        { }

        /// <summary>
        /// Creates a Flame assembly resolver that forwards requests
        /// to a Mono.Cecil assembly resolver.
        /// </summary>
        /// <param name="resolver">The Mono.Cecil assembly resolver to use.</param>
        /// <param name="typeEnvironment">The Flame type environment to use when analyzing assemblies.</param>
        /// <param name="parameters">The Mono.Cecil reader parameters to use.</param>
        public CecilAssemblyResolver(
            Mono.Cecil.IAssemblyResolver resolver,
            TypeEnvironment typeEnvironment,
            Mono.Cecil.ReaderParameters parameters)
        {
            this.Resolver = resolver;
            this.Parameters = parameters;
            this.ReferenceResolver = new ReferenceResolver(this, typeEnvironment);
            this.assemblyCache = new Dictionary<string, IAssembly>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the Mono.Cecil assembly resolver to which requests are forwarded.
        /// </summary>
        public Mono.Cecil.IAssemblyResolver Resolver { get; private set; }

        /// <summary>
        /// Gets the Mono.Cecil reader parameters to use.
        /// </summary>
        public Mono.Cecil.ReaderParameters Parameters { get; private set; }

        /// <summary>
        /// Gets the Flame type environment to use when analyzing assemblies.
        /// </summary>
        public TypeEnvironment TypeEnvironment => ReferenceResolver.TypeEnvironment;

        /// <summary>
        /// Gets the Flame reference resolver for this assembly resolver.
        /// </summary>
        public ReferenceResolver ReferenceResolver { get; private set; }

        private Dictionary<string, IAssembly> assemblyCache;

        /// <inheritdoc/>
        public override bool TryResolve(
            AssemblyIdentity identity,
            out IAssembly assembly)
        {
            if (assemblyCache.TryGetValue(identity.Name, out assembly))
            {
                return true;
            }

            var nameRef = new Mono.Cecil.AssemblyNameReference(identity.Name, identity.VersionOrNull);
            try
            {
                var asmDef = ResolveAssemblyDefinition(nameRef);
                assembly = ClrAssembly.Wrap(asmDef, ReferenceResolver);
                assemblyCache[identity.Name] = assembly;
                return true;
            }
            catch (Mono.Cecil.AssemblyResolutionException)
            {
                assembly = null;
                return false;
            }
        }

        private Mono.Cecil.AssemblyDefinition ResolveAssemblyDefinition(
            Mono.Cecil.AssemblyNameReference nameRef)
        {
            try
            {
                return Resolver.Resolve(nameRef, Parameters);
            }
            catch (Mono.Cecil.AssemblyResolutionException)
            {
                var fallbackPath = FindTrustedPlatformAssembly(nameRef.Name);
                if (fallbackPath == null)
                {
                    throw;
                }

                var readerParameters = new Mono.Cecil.ReaderParameters
                {
                    AssemblyResolver = Resolver,
                    InMemory = Parameters.InMemory,
                    ReadSymbols = Parameters.ReadSymbols,
                    ReadingMode = Parameters.ReadingMode,
                    ReadWrite = Parameters.ReadWrite,
                    ThrowIfSymbolsAreNotMatching = Parameters.ThrowIfSymbolsAreNotMatching
                };
                return Mono.Cecil.AssemblyDefinition.ReadAssembly(fallbackPath, readerParameters);
            }
        }

        private static string FindTrustedPlatformAssembly(string assemblyName)
        {
            var tpa = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
            if (string.IsNullOrEmpty(tpa))
            {
                return null;
            }

            foreach (var path in tpa.Split(Path.PathSeparator))
            {
                if (string.Equals(
                    Path.GetFileNameWithoutExtension(path),
                    assemblyName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }
            return null;
        }
    }
}
