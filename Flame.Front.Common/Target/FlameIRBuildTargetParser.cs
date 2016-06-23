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
        #region Platform identifiers

        public const string IRIdentifier = "ir";

        public IEnumerable<string> PlatformIdentifiers
        {
            get { return new string[] { IRIdentifier }; }
        }

        public bool MatchesPlatformIdentifier(string Identifier)
        {
            return Identifier.Split('/', '\\').First().Equals("ir", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Runtime resolution

        public string GetRuntimeIdentifier(string Identifier, ICompilerLog Log)
        {
            return IRIdentifier;
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

            var asm = new IRAssemblyBuilder(new IRSignature(new SimpleName(Info.Name)), DependencyBuilder.Environment, encoding, Info.Version);

            return new BuildTarget(asm, DependencyBuilder, extension, true);
        }

        #endregion
    }
}
