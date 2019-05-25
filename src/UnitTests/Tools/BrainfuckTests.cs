using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Loyc.MiniTest;

namespace UnitTests
{
    [TestFixture]
    public sealed class BrainfuckTests
    {
        [Test]
        public void RunTests()
        {
            foreach (var file in Directory.GetFiles(
                Path.Combine(ILOptTests.ToolTestPath, "Brainfuck"),
                "*.bf",
                SearchOption.TopDirectoryOnly))
            {
                Console.WriteLine($" - {Path.GetFileName(file)}");
                CompileOptimizeAndRun(file, ILOptTests.RunCommand);
            }
        }

        /// <summary>
        /// Compiles, optimizes and runs a file at a particular location.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to compile, optimize and run.
        /// </param>
        /// <param name="runCommand">
        /// Runs a command, taking the command itself and a
        /// path to an executable as arguments.
        /// </param>
        public static void CompileOptimizeAndRun(
            string fileName,
            Func<ToolCommand, string, string> runCommand)
        {
            var prefix = ILOptTests.CreateTemporaryPath();
            var exePath = prefix + ".exe";
            try
            {
                CompileBrainfuck(fileName, exePath);
                var commands = ILOptTests.ReadCommands(fileName);
                foreach (var command in commands)
                {
                    runCommand(command, exePath);
                }
            }
            finally
            {
                File.Delete(exePath);
            }
        }

        /// <summary>
        /// Compiles the Brainfuck file at a particular path to an
        /// executable.
        /// </summary>
        /// <param name="inputPath">The file to compile.</param>
        /// <param name="outputPath">The path to store the exe at.</param>
        public static void CompileBrainfuck(
            string inputPath,
            string outputPath)
        {
            string stdout, stderr;
            int exitCode = ILOptTests.RunExeLite(
                global::Flame.Brainfuck.Program.Main,
                new[] { inputPath, "-o", outputPath }, // $"\"{inputPath}\" \"-o{outputPath}\"",
                out stdout,
                out stderr);

            if (exitCode != 0)
            {
                throw new Exception($"Error while compiling {inputPath}: {stderr}");
            }
        }
    }
}
