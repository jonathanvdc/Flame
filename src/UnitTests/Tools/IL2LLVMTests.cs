using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Loyc.MiniTest;

namespace UnitTests
{
    [TestFixture]
    public sealed class IL2LLVMTests
    {
        [Test]
        public void RunTests()
        {
            foreach (var file in Directory.GetFiles(
                Path.Combine(ILOptTests.ToolTestPath, "IL2LLVM"),
                "*.cs",
                SearchOption.TopDirectoryOnly))
            {
                Console.WriteLine($" - {Path.GetFileName(file)}");
                CompileAndRun(file, "/optimize+ /unsafe", ILOptTests.RunCommand);
            }
        }

        /// <summary>
        /// Compiles, optimizes and runs a file at a particular location.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to compile, optimize and run.
        /// </param>
        /// <param name="csharpFlags">
        /// Additional flags to pass to C# compiler.
        /// </param>
        /// <param name="runCommand">
        /// Runs a command, taking the command itself, a
        /// path to an executable and a Boolean specifying whether
        /// the executable should be run by the CLR as arguments.
        /// </param>
        public static void CompileAndRun(
            string fileName,
            string csharpFlags,
            Func<ToolCommand, string, bool, string> runCommand)
        {
            var prefix = ILOptTests.CreateTemporaryPath();
            var exePath = prefix + ".exe";
            var irPath = prefix + ".ll";
            var outPath = prefix + ".out";
            try
            {
                ILOptTests.CompileCSharp(fileName, exePath, csharpFlags);
                CompileILToLLVM(exePath, irPath);
                CompileLLVM(irPath, outPath, "");
                var commands = ILOptTests.ReadCommands(fileName);
                foreach (var command in commands)
                {
                    var regularOutput = runCommand(command, exePath, true);
                    var optOutput = runCommand(command, outPath, false);
                    Assert.AreEqual(regularOutput, optOutput);
                }
            }
            finally
            {
                File.Delete(exePath);
                File.Delete(irPath);
                File.Delete(outPath);
            }
        }

        /// <summary>
        /// Compiles an IL assembly at a particular path to LLVM IR.
        /// </summary>
        /// <param name="inputPath">The assembly to optimize.</param>
        /// <param name="outputPath">The path to store the LLVM IR at.</param>
        public static void CompileILToLLVM(
            string inputPath,
            string outputPath)
        {
            string stdout, stderr;
            int exitCode = ILOptTests.RunExeLite(
                IL2LLVM.Program.Main,
                new[] { inputPath, "-o", outputPath }, // $"\"{inputPath}\" \"-o{outputPath}\"",
                out stdout,
                out stderr);

            if (exitCode != 0)
            {
                throw new Exception($"Error while compiling {inputPath}: {stderr}");
            }
        }

        /// <summary>
        /// Compiles the LLVM IR at a particular path to an
        /// executable.
        /// </summary>
        /// <param name="inputPath">The file to compile.</param>
        /// <param name="outputPath">The path to store the executable at.</param>
        /// <param name="flags">Additional flags to pass to the compiler.</param>
        /// <param name="compilerName">The name of the compiler to use.</param>
        public static void CompileLLVM(
            string inputPath,
            string outputPath,
            string flags,
            string compilerName = null)
        {
            if (compilerName == null)
            {
                compilerName = Program.parsedOptions.GetValue<string>(Options.ClangPath);
            }

            string stdout, stderr;
            int exitCode = ILOptTests.RunProcess(
                compilerName,
                $"\"-o{outputPath}\" {flags} \"{inputPath}\"",
                out stdout,
                out stderr);

            if (exitCode != 0)
            {
                throw new Exception($"Error while compiling {inputPath}: {stderr}{stdout}");
            }
        }
    }
}
