using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    /// <summary>
    /// Represents the execution state over time, especially variable access.
    /// </summary>
    public class VariableMetrics
    {
        private VariableMetrics(AccessRecord<IAnalyzedVariable, int> Loaded, AccessRecord<IAnalyzedVariable, int> Stored, IEnumerable<IAnalyzedVariable> Returns, AnalyzedFlow Flow)
        {
            this.Loaded = Loaded;
            this.Stored = Stored;
            this.Returns = Returns;
            this.Flow = Flow;
        }

        public VariableMetrics()
        {
            this.Loaded = new AccessRecord<IAnalyzedVariable, int>();
            this.Stored = new AccessRecord<IAnalyzedVariable, int>();
            this.Returns = new IAnalyzedVariable[0];
            this.Flow = new AnalyzedFlow();
        }
        public VariableMetrics(IEnumerable<IAnalyzedVariable> Loaded, IEnumerable<IAnalyzedVariable> Stored, IEnumerable<IAnalyzedVariable> Returns, AnalyzedFlow Flow)
        {
            this.Loaded = new AccessRecord<IAnalyzedVariable, int>(Loaded, 0);
            this.Stored = new AccessRecord<IAnalyzedVariable, int>(Stored, 0);
            this.Returns = Returns;
            this.Flow = Flow;
        }

        /// <summary>
        /// Gets the current flow control structure.
        /// </summary>
        public AnalyzedFlow Flow { get; private set; }

        /// <summary>
        /// Gets a read-only dictionary that maps variables to the index when they were last retrieved.
        /// </summary>
        public AccessRecord<IAnalyzedVariable, int> Loaded { get; private set; }
        /// <summary>
        /// Gets a read-only dictionary that maps variables to the index when they were last stored.
        /// </summary>
        public AccessRecord<IAnalyzedVariable, int> Stored { get; private set; }

        /// <summary>
        /// Gets a sequence of variables that will be "returned" by this block.
        /// </summary>
        /// <example>
        /// Variable 'a' in
        /// <code>
        /// var b = a;
        /// </code>
        /// is determined to be "returned", i.e., it will be loaded by the next block that uses it, unless
        /// it is explicitly discarded, as in
        /// <code>
        /// a;
        /// </code>
        /// </example>
        public IEnumerable<IAnalyzedVariable> Returns { get; private set; }

        /// <summary>
        /// Gets a sequence that contains all variables that have been loaded so far.
        /// </summary>
        public IEnumerable<IAnalyzedVariable> LoadedVariables
        {
            get
            {
                return this.Loaded.AllAccessedItems;
            }
        }

        /// <summary>
        /// Gets a sequence that contains all variables that have been stored so far.
        /// </summary>
        public IEnumerable<IAnalyzedVariable> StoredVariables
        {
            get
            {
                return this.Stored.AllAccessedItems;
            }
        }
        
        /// <summary>
        /// Gets a sequence containing all variables that have so far been stored, loaded or returned.
        /// </summary>
        public IEnumerable<IAnalyzedVariable> AccessedVariables
        {
            get
            {
                return this.Loaded.AllAccessedItems.Union(Stored.AllAccessedItems).Union(Returns);
            }
        }

        /// <summary>
        /// Returns a boolean value that indicates if this set of variable is in fact empty.
        /// Empty variable metrics correspond to an empty load record, empty store record and no return variables.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return this.Loaded.IsEmpty && this.Stored.IsEmpty && !this.Returns.Any();
            }
        }

        public bool LoadedSince(IAnalyzedVariable Variable, VariableMetrics Point)
        {
            return this.Loaded.AccessedSince(Variable, Point.Loaded);
        }

        public bool StoredSince(IAnalyzedVariable Variable, VariableMetrics Point)
        {
            return this.Stored.AccessedSince(Variable, Point.Stored);
        }

        public bool StoredSince(IEnumerable<IAnalyzedVariable> Variables, VariableMetrics Point)
        {
            return Variables.Any((item) => this.Stored.AccessedSince(item, Point.Stored));
        }

        /// <summary>
        /// Creates new variable metrics based on a chronological relationship: the events in these variable metrics are said to have happened first, followed by the given variable metrics.
        /// </summary>
        /// <param name="Other"></param>
        /// <returns></returns>
        public VariableMetrics Pipe(VariableMetrics Other)
        {
            var loaded = this.Loaded.PipeAccess(this.Returns).PipeTimeline(Other.Loaded);
            var stored = this.Stored.PipeEmpty().PipeTimeline(Other.Stored);
            return new VariableMetrics(loaded, stored, Other.Returns, Flow);
        }

        /// <summary>
        /// Pipes a return action that trails these variable metrics.
        /// </summary>
        /// <param name="Returned"></param>
        /// <returns></returns>
        public VariableMetrics PipeReturns(params IAnalyzedVariable[] Returned)
        {
            var lded = this.Loaded.PipeAccess(this.Returns);
            var stred = this.Stored.PipeEmpty();
            return new VariableMetrics(lded, stred, Returned, Flow);
        }

        /// <summary>
        /// Pipes a store action that trails these variable metrics.
        /// </summary>
        /// <param name="Token"></param>
        /// <returns></returns>
        public VariableMetrics PipeStore(IAnalyzedVariable Token)
        {
            var lded = this.Loaded.PipeAccess(this.Returns);
            var stred = this.Stored.PipeAccess(new IAnalyzedVariable[] { Token });
            return new VariableMetrics(lded, stred, Enumerable.Empty<IAnalyzedVariable>(), Flow);
        }

        /// <summary>
        /// Pushes a flow control structure on the execution state based on the given assertion.
        /// </summary>
        /// <param name="Assertion"></param>
        /// <returns></returns>
        public VariableMetrics PushFlow(IAssertion Assertion)
        {
            return new VariableMetrics(Loaded, Stored, Returns, new AnalyzedFlow(Assertion, Flow));
        }

        /// <summary>
        /// "Pops" the current flow control structure, returning to its parent, if possible.
        /// Otherwise, the current root structure is left unchanged.
        /// </summary>
        /// <returns></returns>
        public VariableMetrics PopFlow()
        {
            if (Flow.IsRoot)
            {
                return this;
            }
            else
            {
                return new VariableMetrics(Loaded, Stored, Returns, Flow.Parent);
            }
        }

        /// <summary>
        /// Creates volatile variable metrics. All known variables are assumed to have changed.
        /// </summary>
        /// <returns></returns>
        public VariableMetrics MakeVolatile()
        {
            var allAccessed = this.AccessedVariables.ToArray();
            var lded = this.Loaded.PipeAccess(allAccessed);
            var stred = this.Stored.PipeAccess(allAccessed);
            return new VariableMetrics(lded, stred, new IAnalyzedVariable[0], Flow);
        }

        /// <summary>
        /// Creates a new set of variable metrics that represent these metrics as an atomic operation.
        /// </summary>
        /// <returns></returns>
        public VariableMetrics MakeAtomic()
        {
            return new VariableMetrics(LoadedVariables, StoredVariables, Returns, Flow);
        }

        /// <summary>
        /// Creates a union of this set of variable metrics and another set of metrics.
        /// </summary>
        /// <returns>
        /// Unless the actions in this set of variable metrics and those in the other set must occur simultaneously, 
        /// it is advisable to use <see cref="M:Flame.Analysis.VariableMetrics.Pipe(VariableMetrics)"/> instead,
        /// as it does a better job at preserving the chronological relationship between variable access.
        /// </returns>
        public VariableMetrics Union(VariableMetrics Other)
        {
            if (this.IsEmpty)
            {
                return Other;
            }
            else if (Other.IsEmpty)
            {
                return this;
            }
            else
            {
                // This is a pretty hackish method of creating the "union" of two sets of variable metrics,
                // and could probably be implemented to somewhat preserve the variable access chronology
                return this.Pipe(Other).MakeAtomic();
            }
        }

        public static VariableMetrics CreateFromReturns(params IAnalyzedVariable[] Tokens)
        {
            return new VariableMetrics(new IAnalyzedVariable[0], new IAnalyzedVariable[0], Tokens, new AnalyzedFlow());
        }
    }
}
