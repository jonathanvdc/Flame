using Flame;
using Flame.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Flame.Compiler;
using dsc.Target;
using Flame.Front;

namespace dsc
{
    public class CecilReferenceResolver : IAssemblyResolver
    {
        private static string[] SecondaryExtensions = new[] { "pdb", "xml" }; // Copy debugging files and xml docs

        public async Task<IAssembly> ResolveAsync(PathIdentifier Identifier, IDependencyBuilder DependencyBuilder)
        {
            var absPath = Identifier.AbsolutePath.Path;
            if (!File.Exists(absPath))
            {
                Program.CompilerLog.LogError(new LogEntry("File not found", "File '" + Identifier.AbsolutePath + "' could not be found."));
            }
            var readerParams = new Mono.Cecil.ReaderParameters();
            readerParams.AssemblyResolver = DependencyBuilder.GetCecilResolver();
            return new CecilAssembly(Mono.Cecil.AssemblyDefinition.ReadAssembly(absPath, readerParams));
        }

        public Task CopyAsync(PathIdentifier SourceIdentifier, PathIdentifier TargetIdentifier)
        {
            var allTasks = new List<Task>();
            allTasks.Add(CopyFileAsync(SourceIdentifier, TargetIdentifier));

            var sourceDirPath = SourceIdentifier.Parent;
            var sourceFileName = SourceIdentifier.NameWithoutExtension;
            var targetDirPath = TargetIdentifier.Parent;
            var targetFileName = TargetIdentifier.NameWithoutExtension;

            foreach (var item in SecondaryExtensions)
            {
                var itemSourcePath = sourceDirPath.Combine(sourceFileName + "." + item);
                if (File.Exists(itemSourcePath.AbsolutePath.Path))
                {
                    var itemTargetPath = targetDirPath.Combine(targetFileName + "." + item);
                    allTasks.Add(CopyFileAsync(itemSourcePath, itemTargetPath));
                }
            }
            return Task.WhenAll(allTasks);
        }

        private static async Task CopyFileAsync(PathIdentifier sourcePath, PathIdentifier destinationPath)
        {
            var absSourcePath = sourcePath.AbsolutePath.Path;
            var absTargetPath = destinationPath.AbsolutePath.Path;
            if (absSourcePath != absTargetPath)
            {
                if (!File.Exists(absSourcePath))
                {
                    Program.CompilerLog.LogError(new LogEntry("File not found", "File '" + sourcePath + "' could not be found."));
                }
                string dirName = Path.GetDirectoryName(absTargetPath);
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                using (var source = new FileStream(absSourcePath, FileMode.Open))
                {
                    using (var destination = new FileStream(absTargetPath, FileMode.Create))
                    {
                        await source.CopyToAsync(destination);
                    }
                }
            }
        }
    }
}
