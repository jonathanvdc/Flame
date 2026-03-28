using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public sealed class ILOptTests
    {
        [Test]
        public void RunBitshiftTest()
        {
            CompileOptimizeAndRunNamedTest("bitshift.cs");
        }

        [Test]
        public void RunBoxTest()
        {
            CompileOptimizeAndRunNamedTest("box.cs");
        }

        [Test]
        public void RunCastclassTest()
        {
            CompileOptimizeAndRunNamedTest("castclass.cs");
        }

        [Test]
        public void RunCompareExchangeTest()
        {
            CompileOptimizeAndRunNamedTest("compare-exchange.cs");
        }

        [Test]
        public void RunConstrainedCallTest()
        {
            CompileOptimizeAndRunNamedTest("constrained-call.cs");
        }

        [Test]
        public void RunConstrainedCall2Test()
        {
            CompileOptimizeAndRunNamedTest("constrained-call2.cs");
        }

        [Test]
        public void RunConvR4Test()
        {
            CompileOptimizeAndRunNamedTest("conv-r4.cs");
        }

        [Test]
        public void RunConvR8Test()
        {
            CompileOptimizeAndRunNamedTest("conv-r8.cs");
        }

        [Test]
        public void RunConvU8Test()
        {
            CompileOptimizeAndRunNamedTest("conv-u8.cs");
        }

        [Test]
        public void RunDynamicTest()
        {
            CompileOptimizeAndRunNamedTest("dynamic.cs");
        }

        [Test]
        public void RunEnumTest()
        {
            CompileOptimizeAndRunNamedTest("enum.cs");
        }

        [Test]
        public void RunFactorialRecursiveTest()
        {
            CompileOptimizeAndRunNamedTest("factorial-recursive.cs");
        }

        [Test]
        public void RunFactorialTest()
        {
            CompileOptimizeAndRunNamedTest("factorial.cs");
        }

        [Test]
        public void RunFixedArrayCopyTest()
        {
            CompileOptimizeAndRunNamedTest("fixed-array-copy.cs");
        }

        [Test]
        public void RunFixedArrayTest()
        {
            CompileOptimizeAndRunNamedTest("fixed-array.cs");
        }

        [Test]
        public void RunFixedStringTest()
        {
            CompileOptimizeAndRunNamedTest("fixed-string.cs");
        }

        [Test]
        public void RunFixedTest()
        {
            CompileOptimizeAndRunNamedTest("fixed.cs");
        }

        [Test]
        public void RunForeachTest()
        {
            CompileOptimizeAndRunNamedTest("foreach.cs");
        }

        [Test]
        public void RunFormatTest()
        {
            CompileOptimizeAndRunNamedTest("format.cs");
        }

        [Test]
        public void RunGenericConstraintsTest()
        {
            CompileOptimizeAndRunNamedTest("generic-constraints.cs");
        }

        [Test]
        public void RunGenericConstraints2Test()
        {
            CompileOptimizeAndRunNamedTest("generic-constraints2.cs");
        }

        [Test]
        public void RunGenericFieldTest()
        {
            CompileOptimizeAndRunNamedTest("generic-field.cs");
        }

        [Test]
        public void RunGenericListTest()
        {
            CompileOptimizeAndRunNamedTest("generic-list.cs");
        }

        [Test]
        public void RunGenericMethodTest()
        {
            CompileOptimizeAndRunNamedTest("generic-method.cs");
        }

        [Test]
        public void RunHeapSortingReorderTest()
        {
            CompileOptimizeAndRunNamedTest("heap-sorting-reorder.cs");
        }

        [Test]
        public void RunHeapSortingTest()
        {
            CompileOptimizeAndRunNamedTest("heap-sorting.cs");
        }

        [Test]
        public void RunInitobjArrayTest()
        {
            CompileOptimizeAndRunNamedTest("initobj-array.cs");
        }

        [Test]
        public void RunInitobjTest()
        {
            CompileOptimizeAndRunNamedTest("initobj.cs");
        }

        [Test]
        public void RunIsinstTest()
        {
            CompileOptimizeAndRunNamedTest("isinst.cs");
        }

        [Test]
        public void RunLambdaTest()
        {
            CompileOptimizeAndRunNamedTest("lambda.cs");
        }

        [Test]
        public void RunLdcR4Test()
        {
            CompileOptimizeAndRunNamedTest("ldc-r4.cs");
        }

        [Test]
        public void RunLdcR8Test()
        {
            CompileOptimizeAndRunNamedTest("ldc-r8.cs");
        }

        [Test]
        public void RunLdelemTest()
        {
            CompileOptimizeAndRunNamedTest("ldelem.cs");
        }

        [Test]
        public void RunLdfldaTest()
        {
            CompileOptimizeAndRunNamedTest("ldflda.cs");
        }

        [Test]
        public void RunLdindI4Test()
        {
            CompileOptimizeAndRunNamedTest("ldind-i4.cs");
        }

        [Test]
        public void RunLdlenTest()
        {
            CompileOptimizeAndRunNamedTest("ldlen.cs");
        }

        [Test]
        public void RunLdsfldStsfldTest()
        {
            CompileOptimizeAndRunNamedTest("ldsfld-stsfld.cs");
        }

        [Test]
        public void RunLinqSelectTest()
        {
            CompileOptimizeAndRunNamedTest("linq-select.cs");
        }

        [Test]
        public void RunLinqToarrayTest()
        {
            CompileOptimizeAndRunNamedTest("linq-toarray.cs");
        }

        [Test]
        public void RunLinqWhereTest()
        {
            CompileOptimizeAndRunNamedTest("linq-where.cs");
        }

        [Test]
        public void RunMergeSortTest()
        {
            CompileOptimizeAndRunNamedTest("merge-sort.cs");
        }

        [Test]
        public void RunNewDelegateTest()
        {
            CompileOptimizeAndRunNamedTest("new-delegate.cs");
        }

        [Test]
        public void RunNewarrTest()
        {
            CompileOptimizeAndRunNamedTest("newarr.cs");
        }

        [Test]
        public void RunNewobjTest()
        {
            CompileOptimizeAndRunNamedTest("newobj.cs");
        }

        [Test]
        public void RunPartialScalarReplTest()
        {
            CompileOptimizeAndRunNamedTest("partial-scalarrepl.cs");
        }

        [Test]
        public void RunPointAddTest()
        {
            CompileOptimizeAndRunNamedTest("point-add.cs");
        }

        [Test]
        public void RunPointTest()
        {
            CompileOptimizeAndRunNamedTest("point.cs");
        }

        [Test]
        public void RunRaytraceDotTest()
        {
            CompileOptimizeAndRunNamedTest("raytrace-dot.cs");
        }

        [Test]
        public void RunRaytraceIntersectTest()
        {
            CompileOptimizeAndRunNamedTest("raytrace-intersect.cs");
        }

        [Test]
        public void RunRaytraceReorderCodegenTest()
        {
            CompileOptimizeAndRunNamedTest("raytrace-reorder-codegen.cs");
        }

        [Test]
        public void RunRaytraceTest()
        {
            CompileOptimizeAndRunNamedTest("raytrace.cs");
        }

        [Test]
        public void RunReordering2Test()
        {
            CompileOptimizeAndRunNamedTest("reordering-2.cs");
        }

        [Test]
        public void RunReorderingTest()
        {
            CompileOptimizeAndRunNamedTest("reordering.cs");
        }

        [Test]
        public void RunScalarReplTest()
        {
            CompileOptimizeAndRunNamedTest("scalarrepl.cs");
        }

        [Test]
        public void RunSizeofTest()
        {
            CompileOptimizeAndRunNamedTest("sizeof.cs");
        }

        [Test]
        public void RunStackallocTest()
        {
            CompileOptimizeAndRunNamedTest("stackalloc.cs");
        }

        [Test]
        public void RunStelemTest()
        {
            CompileOptimizeAndRunNamedTest("stelem.cs");
        }

        [Test]
        public void RunStringConcatTest()
        {
            CompileOptimizeAndRunNamedTest("string-concat.cs");
        }

        [Test]
        public void RunStructFieldsTest()
        {
            CompileOptimizeAndRunNamedTest("struct-fields.cs");
        }

        [Test]
        public void RunStructNewobjTest()
        {
            CompileOptimizeAndRunNamedTest("struct-newobj.cs");
        }

        [Test]
        public void RunStructNewobj2Test()
        {
            CompileOptimizeAndRunNamedTest("struct-newobj2.cs");
        }

        [Test]
        public void RunTernaryImplicitCastTest()
        {
            CompileOptimizeAndRunNamedTest("ternary-implicit-cast.cs");
        }

        [Test]
        public void RunTernaryTest()
        {
            CompileOptimizeAndRunNamedTest("ternary.cs");
        }

        [Test]
        public void RunTryCatchRethrowTest()
        {
            CompileOptimizeAndRunNamedTest("try-catch-rethrow.cs");
        }

        [Test]
        public void RunTryCatchTest()
        {
            CompileOptimizeAndRunNamedTest("try-catch.cs");
        }

        [Test]
        public void RunTryFinallyTest()
        {
            CompileOptimizeAndRunNamedTest("try-finally.cs");
        }

        [Test]
        public void RunTypeofTest()
        {
            CompileOptimizeAndRunNamedTest("typeof.cs");
        }

        [Test]
        public void RunUnaryTest()
        {
            CompileOptimizeAndRunNamedTest("unary.cs");
        }

        [Test]
        public void RunUnboxAnyTest()
        {
            CompileOptimizeAndRunNamedTest("unbox-any.cs");
        }

        [Test]
        public void RunVolatileTest()
        {
            CompileOptimizeAndRunNamedTest("volatile.cs");
        }

        [Test]
        public void RunYieldReturnTest()
        {
            CompileOptimizeAndRunNamedTest("yield-return.cs");
        }

        /// <summary>
        /// Compiles, optimizes, and runs the named ILOpt test file, both with and
        /// without optimization enabled.
        /// </summary>
        /// <param name="fileName">The name of the .cs file in the ILOpt test directory.</param>
        private static void CompileOptimizeAndRunNamedTest(string fileName)
        {
            var file = Path.Combine(ToolTestPath, "ILOpt", fileName);
            CompileOptimizeAndRun(file, "/optimize+ /unsafe", RunCommand);
            CompileOptimizeAndRun(file, "/optimize- /unsafe", RunCommand);
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
            var exePath = prefix + ".dll";
            var optExePath = prefix + ".opt.dll";
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
                DeleteManagedArtifactSet(exePath);
                DeleteManagedArtifactSet(optExePath);
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
            var projectDir = outputPath + ".build";
            Directory.CreateDirectory(projectDir);
            try
            {
                var sourceFileName = Path.GetFileName(inputPath);
                var sourcePath = Path.Combine(projectDir, sourceFileName);
                File.Copy(inputPath, sourcePath, true);

                var assemblyName = Path.GetFileNameWithoutExtension(outputPath);
                var outputDir = Path.GetDirectoryName(outputPath);
                var objDir = Path.Combine(projectDir, "obj");
                var projectPath = Path.Combine(projectDir, assemblyName + ".csproj");

                File.WriteAllText(
                    projectPath,
                    CreateTemporaryProjectFile(
                        assemblyName,
                        outputDir,
                        objDir,
                        sourceFileName,
                        flags));

                string stdout, stderr;
                var exitCode = RunProcess(
                    "dotnet",
                    $"build \"{projectPath}\" -c Release /nologo /verbosity:quiet",
                    out stdout,
                    out stderr);

                if (exitCode != 0)
                {
                    throw new Exception($"Error while compiling {inputPath}: {stderr}{stdout}");
                }
            }
            finally
            {
                DeleteDirectory(projectDir);
            }
        }

        internal static readonly string SourceRootPath = FindSourceRoot();

        internal static readonly string ProjectPath = Path.Combine(SourceRootPath, "UnitTests");

        internal static readonly string ToolTestPath = Path.Combine(
            Directory.GetParent(SourceRootPath).FullName, "tool-tests");

        private static readonly string ILOptDir = Path.Combine(
            SourceRootPath,
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
            var publishAppHostPath = Path.Combine(AppContext.BaseDirectory, appHostName);
            if (File.Exists(publishAppHostPath))
            {
                return publishAppHostPath;
            }

            var publishDllPath = Path.Combine(AppContext.BaseDirectory, "ilopt.dll");
            if (File.Exists(publishDllPath))
            {
                return "dotnet";
            }

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

        private static string FindSourceRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                if (File.Exists(Path.Combine(current.FullName, "UnitTests", "UnitTests.csproj")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException(
                "Cannot locate the source root that contains UnitTests/UnitTests.csproj.");
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
                    EnsureRuntimeConfig(exePath);
                    return RunProcess("dotnet", $"exec \"{exePath}\" {arguments}", out stdout, out stderr);
                default:
                    if (NeedsDotNetHost(exePath) || exePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    {
                        EnsureRuntimeConfig(exePath);
                        return RunProcess("dotnet", $"exec \"{exePath}\" {arguments}", out stdout, out stderr);
                    }
                    return RunProcess(exePath, arguments, out stdout, out stderr);
            }
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

        private static string CreateTemporaryProjectFile(
            string assemblyName,
            string outputDir,
            string objDir,
            string sourceFileName,
            string flags)
        {
            var allowUnsafe = flags.Contains("/unsafe", StringComparison.OrdinalIgnoreCase);
            var optimize = !flags.Contains("/optimize-", StringComparison.OrdinalIgnoreCase);

            return new StringBuilder()
                .AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">")
                .AppendLine("  <PropertyGroup>")
                .AppendLine("    <OutputType>Exe</OutputType>")
                .AppendLine("    <TargetFramework>net10.0</TargetFramework>")
                .AppendLine("    <ImplicitUsings>disable</ImplicitUsings>")
                .AppendLine("    <Nullable>disable</Nullable>")
                .AppendLine("    <UseAppHost>false</UseAppHost>")
                .AppendLine("    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>")
                .AppendLine("    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>")
                .AppendLine("    <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>")
                .AppendLine("    <AssemblyName>" + EscapeXml(assemblyName) + "</AssemblyName>")
                .AppendLine("    <OutputPath>" + EscapeXml(EnsureTrailingDirectorySeparator(outputDir)) + "</OutputPath>")
                .AppendLine("    <BaseIntermediateOutputPath>" + EscapeXml(EnsureTrailingDirectorySeparator(objDir)) + "</BaseIntermediateOutputPath>")
                .AppendLine("    <Optimize>" + (optimize ? "true" : "false") + "</Optimize>")
                .AppendLine("    <AllowUnsafeBlocks>" + (allowUnsafe ? "true" : "false") + "</AllowUnsafeBlocks>")
                .AppendLine("  </PropertyGroup>")
                .AppendLine("  <ItemGroup>")
                .AppendLine("    <Compile Include=\"" + EscapeXml(sourceFileName) + "\" />")
                .AppendLine("  </ItemGroup>")
                .AppendLine("</Project>")
                .ToString();
        }

        private static void DeleteManagedArtifactSet(string assemblyPath)
        {
            File.Delete(assemblyPath);
            File.Delete(Path.ChangeExtension(assemblyPath, ".deps.json"));
            File.Delete(Path.ChangeExtension(assemblyPath, ".runtimeconfig.json"));
            File.Delete(Path.ChangeExtension(assemblyPath, ".pdb"));
        }

        private static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        private static string EnsureTrailingDirectorySeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return "." + Path.DirectorySeparatorChar;
            }

            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private static string EscapeXml(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
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
