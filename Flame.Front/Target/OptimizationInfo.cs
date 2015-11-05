using Flame.Compiler;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    [Flags]
    public enum OptimizationMode
    {
        /// <summary>
        /// Perform no optimization at all.
        /// </summary>
        None = 0,
        /// <summary>
        /// Perform minimal optimizations.
        /// </summary>
        Minimal = 1,
        /// <summary>
        /// Perform normal optimizations.
        /// </summary>
        Normal = 2,
        /// <summary>
        /// Perform experimental optimizations.
        /// </summary>
        Experimental = 4,
        /// <summary>
        /// Perform optimizations that reduce code size,
        /// but may impact performance.
        /// </summary>
        Size = 8,
        /// <summary>
        /// Perform optimizations that do not
        /// affect the debugging experience.
        /// </summary>
        Debug = 16,
        /// <summary>
        /// Perform optimizations that make 
        /// assumptions which may not always turn out
        /// to be true, thus creating invalid code.
        /// </summary>
        Dangerous = 32
    }

    /// <summary>
    /// Defines information that pertains to the optimization process.
    /// </summary>
    public class OptimizationInfo
    {
        public OptimizationInfo(ICompilerLog Log)
            : this(Log, GetOptimizationLevel(Log))
        {
        }
        public OptimizationInfo(ICompilerLog Log, OptimizationMode OptimizationLevel)
        {
            this.Log = Log;
            this.OptimizationLevel = OptimizationLevel;
        }

        public ICompilerLog Log { get; private set; }
        public OptimizationMode OptimizationLevel { get; private set; }

        public bool OptimizeMinimal
        {
            get
            {
                return (OptimizationLevel & OptimizationMode.Minimal) == OptimizationMode.Minimal;
            }
        }
        public bool OptimizeNormal
        {
            get
            {
                return (OptimizationLevel & OptimizationMode.Normal) == OptimizationMode.Normal;
            }
        }
        public bool OptimizeExperimental
        {
            get
            {
                return (OptimizationLevel & OptimizationMode.Experimental) == OptimizationMode.Experimental;
            }
        }
        public bool OptimizeSize
        {
            get
            {
                return (OptimizationLevel & OptimizationMode.Size) == OptimizationMode.Size;
            }
        }
        public bool OptimizeDebug
        {
            get
            {
                return (OptimizationLevel & OptimizationMode.Debug) == OptimizationMode.Debug;
            }
        }

        public static OptimizationMode GetOptimizationLevel(ICompilerLog Log)
        {
            var allOptions = new Dictionary<string, OptimizationMode>()
            {
                { "O0", OptimizationMode.None },
                { "O", OptimizationMode.Minimal },
                { "O1", OptimizationMode.Minimal },
                { "O2", OptimizationMode.Minimal | OptimizationMode.Normal },
                { "O3", OptimizationMode.Minimal | OptimizationMode.Normal | OptimizationMode.Experimental },
                { "O4", OptimizationMode.Minimal | OptimizationMode.Normal | OptimizationMode.Experimental },
                { "Ofast", OptimizationMode.Minimal | OptimizationMode.Normal | OptimizationMode.Experimental | OptimizationMode.Dangerous },
                { "Os", OptimizationMode.Minimal | OptimizationMode.Normal | OptimizationMode.Size },
                { "Oz", OptimizationMode.Minimal | OptimizationMode.Normal | OptimizationMode.Experimental | OptimizationMode.Size },
                { "g", OptimizationMode.Debug },
                { "Og", OptimizationMode.Minimal | OptimizationMode.Debug }
            };

            var selectedOptions = allOptions.Where(item => Log.Options.GetOption<bool>(item.Key, false)).Select(item => item.Value);
            if (!selectedOptions.Any())
            {
                return OptimizationMode.Minimal | OptimizationMode.Debug; // Enable `-Og` if nothing else is specified
            }
            else
            {
                return selectedOptions.Aggregate(OptimizationMode.None, (fst, snd) => fst | snd);
            }
        }

        private static Dictionary<OptimizationMode, Tuple<string, string>> optDirs = new Dictionary<OptimizationMode, Tuple<string, string>>()
        {
            { OptimizationMode.Minimal, Tuple.Create("minimal", "O1") },
            { OptimizationMode.Normal, Tuple.Create("normal", "O2") },
            { OptimizationMode.Experimental, Tuple.Create("experimental", "O3") },
            { OptimizationMode.Size, Tuple.Create("size", "Os") },
            { OptimizationMode.Debug, Tuple.Create("debug", "g") },
            { OptimizationMode.Dangerous, Tuple.Create("dangerous", "Ofast") },
        };

        /// <summary>
        /// Gets a sequence of strings that describe which flags of the
        /// optimization mode are on, along with their
        /// corresponding flags.
        /// </summary>
        /// <param name="Mode"></param>
        /// <returns></returns>
        public static IEnumerable<Tuple<string, string>> GetOptimizationDirectives(OptimizationMode Mode)
        {
            return optDirs.Where(item => (item.Key & Mode) == item.Key)
                          .Select(item => item.Value)
                          .DefaultIfEmpty(Tuple.Create("none", "O0"));
        }
    }
}
