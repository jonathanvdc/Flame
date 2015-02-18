using Flame;
using Flame.Cecil;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Flame.Compiler;
using dsc.Target;

namespace dsc
{
    public class CecilReferenceResolver : IAssemblyResolver
    {
        private static string[] SecondaryExtensions = new[] { "pdb", "xml" }; // Copy debugging files and xml docs

        public async Task<IAssembly> ResolveAsync(string Identifier, IDependencyBuilder DependencyBuilder)
        {
            if (!File.Exists(Identifier))
            {
                ConsoleLog.Instance.LogError(new LogEntry("File not found", "File '" + Identifier + "' could not be found."));
            }
            ReaderParameters readerParams = new ReaderParameters();
            readerParams.AssemblyResolver = DependencyBuilder.GetCecilResolver();
            return new CecilAssembly(AssemblyDefinition.ReadAssembly(Identifier, readerParams));
        }

        public Task CopyAsync(string SourceIdentifier, string TargetIdentifier)
        {
            string sourceDirPath = Path.GetDirectoryName(SourceIdentifier);
            string sourceFileName = Path.GetFileNameWithoutExtension(SourceIdentifier);
            string targetDirPath = Path.GetDirectoryName(TargetIdentifier);
            string targetFileName = Path.GetFileNameWithoutExtension(TargetIdentifier);

            var allTasks = new List<Task>();
            allTasks.Add(CopyFileAsync(SourceIdentifier, TargetIdentifier));
            foreach (var item in SecondaryExtensions)
            {
                string itemSourcePath = Path.Combine(sourceDirPath, sourceFileName + "." + item);
                if (File.Exists(itemSourcePath))
                {
                    string itemTargetPath = Path.Combine(targetDirPath, targetFileName + "." + item);
                    allTasks.Add(CopyFileAsync(itemSourcePath, itemTargetPath));
                }
            }
            return Task.WhenAll(allTasks);
        }

        private static async Task CopyFileAsync(string sourcePath, string destinationPath)
        {
            if (Path.GetFullPath(sourcePath) != Path.GetFullPath(destinationPath))
            {
                if (!File.Exists(sourcePath))
                {
                    ConsoleLog.Instance.LogError(new LogEntry("File not found", "File '" + sourcePath + "' could not be found."));
                }
                string dirName = Path.GetDirectoryName(destinationPath);
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                using (var source = new FileStream(sourcePath, FileMode.Open))
                {
                    using (var destination = new FileStream(destinationPath, FileMode.Create))
                    {
                        await source.CopyToAsync(destination);
                    }
                }
            }
        }
    }
}
