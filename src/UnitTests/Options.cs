using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Pixie.Markup;
using Pixie.Options;

namespace UnitTests
{
    /// <summary>
    /// Defines command-line options for the unit test runner.
    /// </summary>
    public static class Options
    {
        /// <summary>
        /// The 'help' option, which prints usage information.
        /// </summary>
        public static readonly FlagOption Help =
            FlagOption.CreateFlagOption(
                new OptionForm[]
                {
                    OptionForm.Short("h"),
                    OptionForm.Long("help")
                })
            .WithDescription(
                "Print a description of the options understood by the unit test runner.");

        /// <summary>
        /// The 'input' pseudo-option, which specifies the assembly to optimize.
        /// </summary>
        public static readonly SequenceOption<string> Input =
            SequenceOption.CreateStringOption(OptionForm.Long("input"))
            .WithDescription("A path to the assembly to optimize.")
            .WithParameters(new SymbolicOptionParameter("path", true));

        private static string DefaultCscPath =
            Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "csc.exe");

        /// <summary>
        /// The 'csc-path' option, which specifies the path to the C# compiler.
        /// </summary>
        public static readonly ValueOption<string> CscPath =
            ValueOption.CreateStringOption(
                    OptionForm.Long("csc-path"),
                    DefaultCscPath)
                .WithDescription(
                    Quotation.QuoteEvenInBold(
                        "The path to the C# compiler. This is ",
                        DefaultCscPath,
                        " by default."))
                .WithParameter(new SymbolicOptionParameter("path"));

        private static string DefaultClangPath = "clang";

        /// <summary>
        /// The 'clang-path' option, which specifies the path to Clang.
        /// </summary>
        public static readonly ValueOption<string> ClangPath =
            ValueOption.CreateStringOption(
                    OptionForm.Long("clang-path"),
                    DefaultClangPath)
                .WithDescription(
                    Quotation.QuoteEvenInBold(
                        "The path to Clang. This is ",
                        DefaultClangPath,
                        " by default."))
                .WithParameter(new SymbolicOptionParameter("path"));

        /// <summary>
        /// A list of all named options understood by ilopt.
        /// </summary>
        public static readonly IReadOnlyList<Option> All =
            new Option[]
        {
            Help,
            CscPath,
            ClangPath
        };
    }
}
