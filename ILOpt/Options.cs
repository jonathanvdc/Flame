using System.Collections.Generic;
using Pixie.Options;

namespace ILOpt
{
    /// <summary>
    /// Defines Pixie command-line options.
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
        /// The 'input' pseudo-option, which specifies the assembly to optimize.
        /// </summary>
        public static readonly ValueOption<string> Input =
            ValueOption.CreateStringOption(
                OptionForm.Long("input"),
                "")
            .WithDescription("A path to the assembly to optimize.")
            .WithParameter(new SymbolicOptionParameter("path"));

        /// <summary>
        /// The 'output' option, which specifies where the optimized assembly goes.
        /// </summary>
        public static readonly ValueOption<string> Output =
            ValueOption.CreateStringOption(
                    new[] { OptionForm.Short("o"), OptionForm.Long("output") },
                    "")
                .WithDescription("The path to the write the optimized assembly to.")
                .WithParameter(new SymbolicOptionParameter("path"));

        /// <summary>
        /// The 'print-ir' option, which prints method body IR.
        /// </summary>
        public static readonly FlagOption PrintIr =
            FlagOption.CreateFlagOption(OptionForm.Long("print-ir"))
                .WithDescription("Prints method bodies as Flame IR. Useful for debugging ilopt.")
                .WithCategory("Debugging");

        /// <summary>
        /// The 'internalize' option, which makes private and protected members
        /// internal and protected-or-internal, respectively.
        /// </summary>
        public static readonly FlagOption Internalize =
            new FlagOption(OptionForm.Short("finternalize"), OptionForm.Short("fno-internalize"), true)
                .WithDescription(
                    "Makes private and protected types, methods and fields " +
                    "internal and protected-or-internal, respectively.")
                .WithCategory("Optimization");

        /// <summary>
        /// A list of all named options understood by ilopt.
        /// </summary>
        public static readonly IReadOnlyList<Option> All =
            new Option[]
        {
            Help,
            Output,
            PrintIr,
            Internalize
        };
    }
}
