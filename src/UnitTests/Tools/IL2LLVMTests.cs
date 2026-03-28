using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    [Category("LLVM")]
    public sealed class IL2LLVMTests
    {
        [Test]
        public void RunArrayInitLibcTest() => CompileAndRunNamedTest("array-init-libc.cs");

        [Test]
        public void RunArrayLibcTest() => CompileAndRunNamedTest("array-libc.cs");

        [Test]
        public void RunComparisonLibcTest() => CompileAndRunNamedTest("comparison-libc.cs");

        [Test]
        public void RunDelegateLibcTest() => CompileAndRunNamedTest("delegate-libc.cs");

        [Test]
        public void RunDynamicCastLibcTest() => CompileAndRunNamedTest("dynamic-cast-libc.cs");

        [Test]
        [Ignore("temporarily disabled pending investigation")]
        public void RunFactorialFloatLibcTest() => CompileAndRunNamedTest("factorial-float-libc.cs");

        [Test]
        public void RunFactorialLibcTest() => CompileAndRunNamedTest("factorial-libc.cs");

        [Test]
        public void RunGenericStructLibcTest() => CompileAndRunNamedTest("generic-struct-libc.cs");

        [Test]
        public void RunHelloLibcTest() => CompileAndRunNamedTest("hello-libc.cs");

        [Test]
        public void RunInterfaceLibcTest() => CompileAndRunNamedTest("interface-libc.cs");

        [Test]
        public void RunNewobjLibcTest() => CompileAndRunNamedTest("newobj-libc.cs");

        [Test]
        public void RunStaticFieldLibcTest() => CompileAndRunNamedTest("static-field-libc.cs");

        [Test]
        public void RunStringLibcTest() => CompileAndRunNamedTest("string-libc.cs");

        [Test]
        public void RunStructLibcTest() => CompileAndRunNamedTest("struct-libc.cs");

        [Test]
        [Ignore("temporarily disabled pending implementation of String.Substring")]
        public void RunSubstringLibcTest() => CompileAndRunNamedTest("substring-libc.cs");

        [Test]
        public void RunSwitchLibcTest() => CompileAndRunNamedTest("switch-libc.cs");

        [Test]
        public void RunVirtualCallLibcTest() => CompileAndRunNamedTest("virtual-call-libc.cs");

        private static void CompileAndRunNamedTest(string fileName)
        {
            var file = Path.Combine(ILOptTests.ToolTestPath, "IL2LLVM", fileName);
            if (!CanRunOnCurrentPlatform(file, out var skipReason))
            {
                Assert.Ignore("Cannot run " + fileName + " on this platform: " + skipReason);
            }

            CompileAndRun(file, "/optimize+ /unsafe", ILOptTests.RunCommand);
        }

        private static bool CanRunOnCurrentPlatform(string file, out string reason)
        {
            var fileText = File.ReadAllText(file);

            if (fileText.Contains("DllImport(\"libc.so.6\")", StringComparison.Ordinal)
                && !RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                reason = "requires Linux libc";
                return false;
            }

            if (fileText.Contains("DllImport(\"kernel32", StringComparison.OrdinalIgnoreCase)
                && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                reason = "requires Windows kernel32";
                return false;
            }

            if (fileText.Contains("DllImport(\"libSystem", StringComparison.OrdinalIgnoreCase)
                && !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                reason = "requires macOS libSystem";
                return false;
            }

            reason = null;
            return true;
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
            var exePath = prefix + ".dll";
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
                File.Delete(Path.ChangeExtension(exePath, ".deps.json"));
                File.Delete(Path.ChangeExtension(exePath, ".runtimeconfig.json"));
                File.Delete(Path.ChangeExtension(exePath, ".pdb"));
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
                compilerName = Environment.GetEnvironmentVariable("CLANG_PATH") ?? "clang";
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
