using System.Collections.Generic;
using Pixie.Options;

namespace Flame.Brainfuck
{
    /// <summary>
    /// Defines Pixie command-line options for the Brainfuck compiler.
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
                "Print a description of the options understood by ilopt.");

        /// <summary>
        /// The 'input' pseudo-option, which specifies the Brainfuck file to compile.
        /// </summary>
        public static readonly ValueOption<string> Input =
            ValueOption.CreateStringOption(
                OptionForm.Long("input"),
                "")
            .WithDescription("A path to the Brainfuck file to compile.")
            .WithParameter(new SymbolicOptionParameter("path"));

        /// <summary>
        /// The 'output' option, which specifies where the optimized assembly goes.
        /// </summary>
        public static readonly ValueOption<string> Output =
            ValueOption.CreateStringOption(
                    new[] { OptionForm.Short("o"), OptionForm.Long("output") },
                    "")
                .WithDescription("The path to the write the compiled assembly to.")
                .WithParameter(new SymbolicOptionParameter("path"));

        /// <summary>
        /// The 'print-ir' option, which prints method body IR.
        /// </summary>
        public static readonly FlagOption PrintIr =
            FlagOption.CreateFlagOption(OptionForm.Long("print-ir"))
                .WithDescription("Prints method bodies as Flame IR. Useful for debugging fbfc.")
                .WithCategory("Debugging");

        /// <summary>
        /// A list of all named options understood by ilopt.
        /// </summary>
        public static readonly IReadOnlyList<Option> All =
            new Option[]
        {
            Help,
            Output,
            PrintIr
        };
    }
}
