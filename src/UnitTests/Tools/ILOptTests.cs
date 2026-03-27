using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Loyc.MiniTest;

namespace UnitTests
{
    [TestFixture]
    public sealed class ILOptTests
    {
        [Test]
        public void RunTests()
        {
            try
            {
                foreach (var file in Directory.GetFiles(
                    Path.Combine(ToolTestPath, "ILOpt"),
                    "*.cs",
                    SearchOption.TopDirectoryOnly))
                {
                    Console.WriteLine($" - {Path.GetFileName(file)} (optimized)");
                    CompileOptimizeAndRun(file, "/optimize+ /unsafe", RunCommand);
                    Console.WriteLine($" - {Path.GetFileName(file)} (not optimized)");
                    CompileOptimizeAndRun(file, "/optimize- /unsafe", RunCommand);
                }
            }
            catch (Exception ex)
            {
                // Explicitly dump the exception.
                Console.Error.WriteLine(ex);
                throw;
            }
        }

        /// <summary>
        /// Runs a command.
        /// </summary>
        /// <param name="command">The command to run.</param>
        /// <param name="exePath">The path of the executable to run.</param>
        /// <returns>Standard output, freshly captured from the executable.</returns>
        public static string RunCommand(ToolCommand command, string exePath)
        {
            return RunCommand(command, exePath, true);
        }

        /// <summary>
        /// Runs a command.
        /// </summary>
        /// <param name="command">The command to run.</param>
        /// <param name="exePath">The path of the executable to run.</param>
        /// <param name="isClrExecutable">Tells if the exe to run should be executed by a CLR implementation.</param>
        /// <returns>Standard output, freshly captured from the executable.</returns>
        public static string RunCommand(ToolCommand command, string exePath, bool isClrExecutable)
        {
            if (command.Command == "run")
            {
                string stdout, stderr;
                int exitCode = isClrExecutable
                    ? RunExe(exePath, command.Argument, out stdout, out stderr)
                    : RunProcess(exePath, command.Argument, out stdout, out stderr);
                if (exitCode != 0)
                {
                    throw new Exception($"Executable at '{exePath}' exited with a nonzero exit code: {stdout}{stderr}");
                }
                if (command.HasExpectedOutput)
                {
                    Assert.AreEqual(command.ExpectedOutput, stdout.Trim());
                }
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
                    if (!OutputsMatch(regularOutput, optOutput))
                    {
                        Assert.AreEqual(regularOutput, optOutput);
                    }
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
                $"\"/out:{outputPath}\" /nologo {flags} \"{inputPath}\"",
                out stdout,
                out stderr);

            if (exitCode != 0)
            {
                throw new Exception($"Error while compiling {inputPath}: {stderr}{stdout}");
            }
        }

        internal static readonly string ProjectPath = Directory.GetParent(
            System.Reflection.Assembly.GetExecutingAssembly().Location).Parent.Parent.Parent.Parent.FullName;

        internal static readonly string ToolTestPath = Path.Combine(
            Directory.GetParent(ProjectPath).FullName, "tool-tests");

        private static readonly string ILOptDir = Path.Combine(
            ProjectPath,
            "ILOpt",
            "bin",
            "Debug",
            "net10.0");

        private static readonly string ILOptPath = GetILOptPath();

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
            var processName = ILOptPath;
            var arguments = $"\"{inputPath}\" -o \"{outputPath}\"";
            if (string.Equals(processName, "dotnet", StringComparison.Ordinal))
            {
                arguments = $"\"{Path.Combine(ILOptDir, "ilopt.dll")}\" " + arguments;
            }

            int exitCode = RunProcess(
                processName,
                arguments,
                out stdout,
                out stderr);

            if (exitCode != 0)
            {
                throw new Exception($"Error while optimizing {inputPath}: {stderr}{stdout}");
            }
        }

        private static string GetILOptPath()
        {
            var appHostName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "ilopt.exe"
                : "ilopt";
            var appHostPath = Path.Combine(ILOptDir, appHostName);
            if (File.Exists(appHostPath))
            {
                return appHostPath;
            }

            var dllPath = Path.Combine(ILOptDir, "ilopt.dll");
            if (File.Exists(dllPath))
            {
                return "dotnet";
            }

            throw new FileNotFoundException("Cannot find the ilopt test executable.", appHostPath);
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
                    if (NeedsDotNetHost(exePath))
                    {
                        if (HasAssemblyReference(exePath, "mscorlib"))
                        {
                            var monoPath = NormalizeMonoAssemblyPath(exePath);
                            var monoExitCode = RunProcess("mono", $"\"{monoPath}\" {arguments}", out stdout, out stderr);
                            if (monoExitCode == 0)
                            {
                                return monoExitCode;
                            }
                        }

                        EnsureRuntimeConfig(exePath);
                        return RunProcess("dotnet", $"exec \"{exePath}\" {arguments}", out stdout, out stderr);
                    }
                    else
                    {
                        var monoPath = NormalizeMonoAssemblyPath(exePath);
                        return RunProcess("mono", $"\"{monoPath}\" {arguments}", out stdout, out stderr);
                    }
                default:
                    return RunProcess(exePath, arguments, out stdout, out stderr);
            }
        }

        private static string NormalizeMonoAssemblyPath(string assemblyPath)
        {
            if (Environment.OSVersion.Platform == PlatformID.MacOSX
                && (assemblyPath.StartsWith("/tmp/", StringComparison.Ordinal)
                    || assemblyPath.StartsWith("/var/", StringComparison.Ordinal)))
            {
                return "/private" + assemblyPath;
            }
            return assemblyPath;
        }

        private static bool OutputsMatch(string left, string right)
        {
            if (left == right)
            {
                return true;
            }

            var leftLines = left.Split('\n');
            var rightLines = right.Split('\n');
            if (leftLines.Length != rightLines.Length)
            {
                return false;
            }

            for (int i = 0; i < leftLines.Length; i++)
            {
                if (leftLines[i] == rightLines[i])
                {
                    continue;
                }

                float leftFloat;
                float rightFloat;
                if (!float.TryParse(leftLines[i], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out leftFloat)
                    || !float.TryParse(rightLines[i], NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out rightFloat)
                    || Math.Abs(leftFloat - rightFloat) > 1e-5f)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool NeedsDotNetHost(string assemblyPath)
        {
            try
            {
                var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(assemblyPath);
                return assembly.MainModule.AssemblyReferences.Any(
                    reference => reference.Name == "System.Private.CoreLib");
            }
            catch
            {
                return false;
            }
        }

        private static bool HasAssemblyReference(string assemblyPath, string assemblyName)
        {
            try
            {
                var assembly = Mono.Cecil.AssemblyDefinition.ReadAssembly(assemblyPath);
                return assembly.MainModule.AssemblyReferences.Any(
                    reference => reference.Name == assemblyName);
            }
            catch
            {
                return false;
            }
        }

        private static void EnsureRuntimeConfig(string assemblyPath)
        {
            var runtimeConfigPath = Path.ChangeExtension(assemblyPath, ".runtimeconfig.json");
            if (File.Exists(runtimeConfigPath))
            {
                return;
            }

            var frameworkVersion = Environment.Version.ToString();
            File.WriteAllText(
                runtimeConfigPath,
                "{\n" +
                "  \"runtimeOptions\": {\n" +
                "    \"tfm\": \"net10.0\",\n" +
                "    \"framework\": {\n" +
                "      \"name\": \"Microsoft.NETCore.App\",\n" +
                "      \"version\": \"" + frameworkVersion + "\"\n" +
                "    }\n" +
                "  }\n" +
                "}\n");
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
                    var splitLine = trimmedLine.Substring("//!".Length).Split(new[] { ':' }, 3);
                    if (splitLine.Length > 2)
                    {
                        commands.Add(new ToolCommand(splitLine[0].Trim(), splitLine[1].Trim(), splitLine[2].Trim()));
                    }
                    else if (splitLine.Length > 1)
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
        public static string CreateTemporaryPath()
        {
            return Path.GetTempPath() + Guid.NewGuid().ToString();
        }
    }
}
