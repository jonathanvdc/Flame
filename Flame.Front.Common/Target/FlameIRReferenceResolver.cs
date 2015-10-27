using Flame.Compiler;
using Flame.Front.Projects;
using Flame.Intermediate.Parsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class FlameIRReferenceResolver : IAssemblyResolver
    {
        private static string[] SecondaryExtensions = new[] { "xml" }; // Copy xml docs

        public Task<IAssembly> ResolveAsync(PathIdentifier Identifier, IDependencyBuilder DependencyBuilder)
        {
            var absPath = Identifier.AbsolutePath.Path;
            if (!File.Exists(absPath))
            {
                DependencyBuilder.Log.LogError(new LogEntry("File not found", "File '" + Identifier.AbsolutePath + "' could not be found."));
            }

            var nodes = FlameIRProjectHandler.ParseFile(Identifier);

            return Task.FromResult<IAssembly>(FlameIRProjectHandler.Parser.ParseAssembly(DependencyBuilder.Binder, nodes));
        }

        public Task<PathIdentifier?> CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier, ICompilerLog Log)
        {
            return CecilReferenceResolver.CopyAsync(SourceIdentifier, TargetIdentifier, Log, SecondaryExtensions);
        }
    }
}
