using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Visitors;

namespace Flame.Front.Passes
{
    /// <summary>
    /// A data structure that stores user preferences and pass conditions.
    /// It then uses that information to select passes.
    /// </summary>
    public class PassSelector
    {
        public PassSelector(
            OptimizationInfo OptInfo, IEnumerable<PassCondition> Conditions)
        {
            this.OptInfo = OptInfo;
            this.conditionDictionary = CreatePassConditionDictionary(Conditions);
        }

        /// <summary>
        /// Gets the general optimization settingss.
        /// </summary>
        public OptimizationInfo OptInfo { get; private set; }

        /// <summary>
        /// Gets the compiler options for this pass selector.
        /// </summary>
        public ICompilerOptions Options { get { return OptInfo.Log.Options; } }

        // Stores the pass condition dictionary.
        private Dictionary<string, List<Func<OptimizationInfo, bool>>> conditionDictionary;

        /// <summary>
        /// Determines whether the pass with the given name has 
        /// been selected by this pass selector.
        /// </summary>
        public bool IsActive(string Name)
        {
            return OptInfo.Log.Options.GetFlag(Name, AnyConditionSatisfied(Name));
        }

        /// <summary>
        /// Determines whether the pass described by the given 
        /// pass info has been selected by this pass selector.
        /// </summary>
        public bool IsActive<TIn, TOut>(PassInfo<TIn, TOut> Info)
        {
            return IsActive(Info.Name);
        }

        /// <summary>
        /// Selects all active passes from the given sequence of passes.
        /// </summary>
        public IReadOnlyList<PassInfo<TIn, TOut>> SelectActive<TIn, TOut>(
            IEnumerable<PassInfo<TIn, TOut>> Info)
        {
            return Info.Where(IsActive).ToArray();
        }

        /// <summary>
        /// Selects all active passes from the given name tree.
        /// </summary>
        public NameTree SelectActive(NameTree Tree)
        {
            return Tree.Where(IsActive);
        }

        /// <summary>
        /// Selects all active passes from the given sequence of passes,
        /// then instantiates them.
        /// </summary>
        public IReadOnlyList<IPass<TIn, TOut>> InstantiateActive<TIn, TOut>(
            IEnumerable<PassInfo<TIn, TOut>> Info)
        {
            return Info.Where(IsActive).Select(item => item.Instantiate(this)).ToArray();
        }

        /// <summary>
        /// Creates a dictionary that maps pass names to a sequence
        /// of sufficient conditions from the given sequence of
        /// pass conditions.
        /// </summary>
        private static Dictionary<string, List<Func<OptimizationInfo, bool>>> CreatePassConditionDictionary(
            IEnumerable<PassCondition> Conditions)
        {
            var results = new Dictionary<string, List<Func<OptimizationInfo, bool>>>();
            foreach (var item in Conditions)
            {
                List<Func<OptimizationInfo, bool>> itemSet;
                if (!results.TryGetValue(item.PassName, out itemSet))
                {
                    itemSet = new List<Func<OptimizationInfo, bool>>();
                    results[item.PassName] = itemSet;
                }
                itemSet.Add(item.Condition);
            }
            return results;
        }

        /// <summary>
        /// Checks if any of the conditions for
        /// the pass with the given name are satisfied
        /// by the given optimization info.
        /// </summary>
        private bool AnyConditionSatisfied(string Name)
        {
            List<Func<OptimizationInfo, bool>> conds;
            if (!conditionDictionary.TryGetValue(Name, out conds))
            {
                return false;
            }
            return conds.Any(item => item(OptInfo));
        }
    }
}

