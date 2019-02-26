using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Loyc.MiniTest;

namespace UnitTests
{
    [TestFixture]
    public sealed class ILOptTests
    {
        [Test]
        public void RunTests()
        {
            foreach (var file in Directory.GetFiles(
                Path.Combine(ProjectPath, "ToolTests", "ILOpt"),
                "*.cs",
                SearchOption.TopDirectoryOnly))
            {
                Console.WriteLine($" - {Path.GetFileName(file)} (optimized)");
                CompileOptimizeAndRun(file, "/optimize+", RunCommand);
                Console.WriteLine($" - {Path.GetFileName(file)} (not optimized)");
                CompileOptimizeAndRun(file, "/optimize-", RunCommand);
            }
        }

        private string RunCommand(ToolCommand command, string exePath)
        {
            if (command.Command == "run")
            {
                string stdout, stderr;
                Assert.AreEqual(0, RunExe(exePath, command.Argument, out stdout, out stderr));
                return stdout;
            }
            else
            {
                throw new NotImplementedException();
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
        /// Runs a command, taking the command itself and a
        /// path to an executable as arguments.
        /// </param>
        public static void CompileOptimizeAndRun(
            string fileName,
            string csharpFlags,
            Func<ToolCommand, string, string> runCommand)
        {
            var prefix = CreateTemporaryPath();
            var exePath = prefix + ".exe";
            var optExePath = prefix + ".opt.exe";
            try
            {
                CompileCSharp(fileName, exePath, csharpFlags);
                OptimizeIL(exePath, optExePath);
                var commands = ReadCommands(fileName);
                foreach (var command in commands)
                {
                    var regularOutput = runCommand(command, exePath);
                    var optOutput = runCommand(command, optExePath);
                    Assert.AreEqual(regularOutput, optOutput);
                }
            }
            finally
            {
                File.Delete(exePath);
                File.Delete(optExePath);
            }
        }

        /// <summary>
        /// Compiles the C# file at a particular path to an
        /// executable.
        /// </summary>
        /// <param name="inputPath">The file to compile.</param>
        /// <param name="outputPath">The path to store the exe at.</param>
        /// <param name="flags">Additional flags to pass to the compiler.</param>
        /// <param name="compilerName">The name of the compiler to use.</param>
        public static void CompileCSharp(
            string inputPath,
            string outputPath,
            string flags,
            string compilerName = null)
        {
            if (compilerName == null)
            {
                compilerName = Program.parsedOptions.GetValue<string>(Options.CscPath);
            }

            string stdout, stderr;
            int exitCode = RunProcess(
                compilerName,
                $"\"{inputPath}\" \"/out:{outputPath}\" /nologo {flags}",
                out stdout,
                out stderr);

            if (exitCode != 0)
            {
                throw new Exception($"Error while compiling {inputPath}: {stderr}{stdout}");
            }

            // Wait for the output file to become available.
            if (!WaitForFile(outputPath))
            {
                throw new Exception($"Output file '{outputPath}' is busy.");
            }
        }

        private static bool IsFileReady(string filePath)
        {
            // This function is based on Gordon Thompson's answer to Refracted Paladin's
            // question on StackOverflow: "Wait for file to be freed by process".
            // https://stackoverflow.com/questions/1406808/wait-for-file-to-be-freed-by-process

            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (var inputStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    return inputStream.Length >= 0;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        /// <summary>
        /// Waits for a particular file to become available.
        /// </summary>
        /// <param name="filePath">The file's path.</param>
        /// <param name="maxTries">The number of availability tests.</param>
        /// <returns>
        /// <c>true</c> if the file has become available; otherwise, <c>false</c>.
        /// </returns>
        private static bool WaitForFile(string filePath, int maxTries = 50)
        {
            for (int i = 0; i < maxTries; i++)
            {
                if (IsFileReady(filePath))
                {
                    return true;
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            return false;
        }

        private static readonly string ProjectPath = Directory.GetParent(
            System.Reflection.Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.FullName;

        private static readonly string ILOptPath = Path.Combine(
            ProjectPath,
            "ILOpt",
            "bin",
            "Debug",
            "ilopt.exe");

        /// <summary>
        /// Optimizes an IL assembly at a particular path and
        /// writes the optimized version to the output path.
        /// </summary>
        /// <param name="inputPath">The assembly to optimize.</param>
        /// <param name="outputPath">The path to store the optimized assembly at.</param>
        public static void OptimizeIL(
            string inputPath,
            string outputPath)
        {
            string stdout, stderr;
            int exitCode = RunExeLite(
                ILOpt.Program.Main,
                new[] { inputPath, "-o", outputPath }, // $"\"{inputPath}\" \"-o{outputPath}\"",
                out stdout,
                out stderr);

            if (exitCode != 0)
            {
                throw new Exception($"Error while optimizing {inputPath}: {stderr}");
            }
        }

        /// <summary>
        /// Runs a process to completion.
        /// </summary>
        /// <param name="processName">The name of the process to run.</param>
        /// <param name="arguments">The process' arguments.</param>
        /// <param name="stdout">The output produced by the process.</param>
        /// <returns>The process' exit code.</returns>
        public static int RunProcess(
            string processName,
            string arguments,
            out string stdout,
            out string stderr)
        {
            using (var process = new Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.FileName = processName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.Start();
                process.WaitForExit();
                stdout = process.StandardOutput.ReadToEnd();
                stderr = process.StandardError.ReadToEnd();
                return process.ExitCode;
            }
        }

        /// <summary>
        /// Runs an executable to completion.
        /// </summary>
        /// <param name="exePath">The name of the process to run.</param>
        /// <param name="arguments">The process' arguments.</param>
        /// <param name="stdout">The output stream produced by the process.</param>
        /// <param name="stderr">The error stream produced by the process.</param>
        /// <returns>The process' exit code.</returns>
        public static int RunExe(
            string exePath,
            string arguments,
            out string stdout,
            out string stderr)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    return RunProcess("mono", $"\"{exePath}\" {arguments}", out stdout, out stderr);
                default:
                    return RunProcess(exePath, arguments, out stdout, out stderr);
            }
        }

        /// <summary>
        /// Runs a function to completion in a semi-isolated environment.
        /// Specifically, the error and output streams are redirected.
        /// </summary>
        /// <param name="runExe">Runs the program.</param>
        /// <param name="arguments">The process' arguments.</param>
        /// <param name="stdout">The output stream produced by the process.</param>
        /// <param name="stderr">The error stream produced by the process.</param>
        /// <returns>The process' exit code.</returns>
        public static int RunExeLite(
            Func<string[], int> runExe,
            string[] arguments,
            out string stdout,
            out string stderr)
        {
            var outWriter = new StringWriter();
            var errWriter = new StringWriter();
            var oldOutWriter = Console.Out;
            var oldErrWriter = Console.Error;
            Console.SetOut(outWriter);
            Console.SetError(errWriter);
            try
            {
                return runExe(arguments);
            }
            finally
            {
                Console.SetOut(oldOutWriter);
                Console.SetError(oldErrWriter);
                stdout = outWriter.ToString();
                stderr = errWriter.ToString();
            }
        }

        /// <summary>
        /// Reads all tool commands from a file.
        /// </summary>
        /// <param name="fileName">The file to read.</param>
        /// <returns>A list of tool commands.</returns>
        public static IReadOnlyList<ToolCommand> ReadCommands(string fileName)
        {
            var commands = new List<ToolCommand>();
            foreach (var line in File.ReadAllLines(fileName))
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("//!", StringComparison.Ordinal))
                {
                    var splitLine = trimmedLine.Substring("//!".Length).Split(new[] { ':' }, 2);
                    if (splitLine.Length > 1)
                    {
                        commands.Add(new ToolCommand(splitLine[0].Trim(), splitLine[1].Trim()));
                    }
                    else
                    {
                        commands.Add(new ToolCommand(splitLine[0].Trim(), ""));
                    }
                }
            }
            return commands;
        }

        /// <summary>
        /// Creates a path at which a temporary file may be created.
        /// </summary>
        /// <returns>A path string.</returns>
        private static string CreateTemporaryPath()
        {
            return Path.GetTempPath() + Guid.NewGuid().ToString();
        }
    }
}
