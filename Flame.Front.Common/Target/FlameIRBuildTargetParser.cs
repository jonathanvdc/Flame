using Flame.Binding;
using Flame.Compiler;
using Flame.Front.Target;
using Flame.Intermediate;
using Flame.Intermediate.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class FlameIRBuildTargetParser : IBuildTargetParser
    {
        #region Indirect platform

        public const string IndirectPlatformKey = "indirect-platform";

        public static readonly WarningDescription IndirectPlatformWarning = new WarningDescription(IndirectPlatformKey, Warnings.Instance.Build);

        public static string GetIndirectPlatformIdentifier(ICompilerOptions Options)
        {
            return Options.GetOption<string>(IndirectPlatformKey, "");
        }

        public static IBuildTargetParser GetIndirectBuildTargetParser(ICompilerOptions Options)
        {
            return BuildTargetParsers.Parser.GetParser(GetIndirectPlatformIdentifier(Options));
        }

        #endregion

        #region Platform identifiers

        public IEnumerable<string> PlatformIdentifiers
        {
            get { return new string[] { "ir", "ir/fir", "ir/flo" }; }
        }

        public bool MatchesPlatformIdentifier(string Identifier)
        {
            return Identifier.Split('/', '\\').First().Equals("ir", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Runtime resolution

        public PlatformRuntime GetRuntime(string Identifier, ICompilerLog Log)
        {
            // Log warnings here, fail silently elsewhere.
            var platformIdent = GetIndirectPlatformIdentifier(Log.Options);
            var indirectParser = BuildTargetParsers.Parser.GetParser(platformIdent);

            if (indirectParser == null)
            {
                if (!string.IsNullOrWhiteSpace(platformIdent) && IndirectPlatformWarning.UseWarning(Log.Options))
                {
                    Log.LogWarning(new LogEntry(
                        "Invalid indirect platform",
                        IndirectPlatformWarning.CreateMessage(
                            "The indirect platform '" + platformIdent + "' " +
                            "was not recognized as a known target platform. ")));
                }
                return new PlatformRuntime("ir", new EmptyAssemblyResolver());
            }
            else if (indirectParser is FlameIRBuildTargetParser)
            {
                if (IndirectPlatformWarning.UseWarning(Log.Options))
                {
                    Log.LogWarning(new LogEntry(
                        "Invalid indirect platform",
                        IndirectPlatformWarning.CreateMessage(
                            "The indirect platform '" + platformIdent + "' " +
                            "cannot be of the same type as the target platform. ")));
                }
                return new PlatformRuntime("ir", new EmptyAssemblyResolver());
            }
            else
            {
                return indirectParser.GetRuntime(Identifier, Log);
            }
        }

        #endregion

        #region Dependency builder

        public IDependencyBuilder CreateDependencyBuilder(string Identifier, IAssemblyResolver RuntimeAssemblyResolver, IAssemblyResolver ExternalResolver, Compiler.ICompilerLog Log, PathIdentifier CurrentPath, PathIdentifier OutputDirectory)
        {
            // Warnings have already been logged. Ignore them here.
            var indirectParser = GetIndirectBuildTargetParser(Log.Options);

            if (indirectParser == null || indirectParser is FlameIRBuildTargetParser)
            {
                return new DependencyBuilder(RuntimeAssemblyResolver, ExternalResolver, EmptyEnvironment.Instance, CurrentPath, OutputDirectory, Log);
            }
            else
            {
                return indirectParser.CreateDependencyBuilder(Identifier, RuntimeAssemblyResolver, ExternalResolver, Log, CurrentPath, OutputDirectory);
            }
        }

        #endregion

        #region Build target creation

        private static IRAssemblyEncoding GetEncoding(string Identifier, AssemblyCreationInfo Info, ICompilerOptions Options)
        {
            switch (Identifier.Substring("ir".Length))
            {
                case "/fir":
                case "/text":
                    return IRAssemblyEncoding.Textual;

                case "/flo":
                case "/binary":
                    return IRAssemblyEncoding.Binary;

                default:
                    return Options.GetOption<bool>(Flags.EmitAssemblyOptionName, false) 
                        ? IRAssemblyEncoding.Textual 
                        : IRAssemblyEncoding.Binary;
            }
        }

        public BuildTarget CreateBuildTarget(string PlatformIdentifier, AssemblyCreationInfo Info, IDependencyBuilder DependencyBuilder)
        {
            var encoding = GetEncoding(PlatformIdentifier, Info, DependencyBuilder.Log.Options);
            string extension = encoding == IRAssemblyEncoding.Binary ? "flo" : "fir";

            var asm = new IRAssemblyBuilder(new IRSignature(Info.Name), DependencyBuilder.Environment, encoding, Info.Version);

            return new BuildTarget(asm, DependencyBuilder, extension, true);
        }

        #endregion
    }
}
