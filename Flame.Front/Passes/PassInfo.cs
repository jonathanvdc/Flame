using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler;
using Flame.Compiler.Visitors;

namespace Flame.Front.Passes
{
    /// <summary>
    /// A data structure that stores information related to some pass.
    /// Passes with equal names are assumed to be equal.
    /// </summary>
    public abstract class PassInfo<TIn, TOut> : IEquatable<PassInfo<TIn, TOut>>
    {
        /// <summary>
        /// Gets the described pass' name.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the pass name tree for this pass info.
        /// </summary>
        public abstract NameTree NameTree { get; }

        /// <summary>
        /// Instantiates the pass that is described by this pass info.
        /// </summary>
        public abstract IPass<TIn, TOut> Instantiate(PassSelector Selector);

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return obj is PassInfo<TIn, TOut> && Equals((PassInfo<TIn, TOut>)obj);
        }

        public bool Equals(PassInfo<TIn, TOut> other)
        {
            return Name == other.Name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// A pass description for atomic passes.
    /// </summary>
    public class AtomicPassInfo<TIn, TOut> : PassInfo<TIn, TOut>
    {
        public AtomicPassInfo(IPass<TIn, TOut> Pass, string Name)
        {
            this.Pass = Pass;
            this.passName = Name;
        }

        private string passName;

        /// <summary>
        /// Gets the pass this pass info structure describes.
        /// </summary>
        public IPass<TIn, TOut> Pass { get; private set; }

        /// <summary>
        /// Gets the pass' name.
        /// </summary>
        public override string Name { get { return passName; } }

        /// <summary>
        /// Gets the pass name tree for this pass info.
        /// </summary>
        public override NameTree NameTree { get { return new NameTree(Name); } }

        public override IPass<TIn, TOut> Instantiate(PassSelector Selector)
        {
            return Pass;
        }
    }

    /// <summary>
    /// A pass description for transformed passes.
    /// </summary>
    public class TransformedPassInfo<TOldIn, TOldOut, TNewIn, TNewOut> : PassInfo<TNewIn, TNewOut>
    {
        public TransformedPassInfo(
            PassInfo<TOldIn, TOldOut> Info,
            Func<IPass<TOldIn, TOldOut>, IPass<TNewIn, TNewOut>> Transform)
        {
            this.Info = Info;
            this.Transform = Transform;
        }

        /// <summary>
        /// Gets the pass this pass info structure describes.
        /// </summary>
        public PassInfo<TOldIn, TOldOut> Info { get; private set; }

        /// <summary>
        /// Transforms the given pass.
        /// </summary>
        public Func<IPass<TOldIn, TOldOut>, IPass<TNewIn, TNewOut>> Transform { get; private set; }

        public override string Name { get { return Info.Name; } }

        /// <summary>
        /// Gets the pass name tree for this pass info.
        /// </summary>
        public override NameTree NameTree { get { return Info.NameTree; } }

        public override IPass<TNewIn, TNewOut> Instantiate(PassSelector Selector)
        {
            return Transform(Info.Instantiate(Selector));
        }
    }

    /// <summary>
    /// A pass description for pass loops.
    /// </summary>
    public class PassLoopInfo : PassInfo<BodyPassArgument, IStatement>
    {
        public PassLoopInfo(
            string Name,
            IReadOnlyList<PassInfo<LoopPassArgument, LoopPassResult>> LoopPasses,
            IReadOnlyList<PassInfo<BodyPassArgument, IStatement>> FinalizationPasses,
            string MaxIterationsOption, int DefaultMaxIterations)
        {
            this.passName = Name;
            this.LoopPasses = LoopPasses;
            this.FinalizationPasses = FinalizationPasses;
            this.MaxIterationsOption = MaxIterationsOption;
            this.DefaultMaxIterations = DefaultMaxIterations;
        }

        public PassLoopInfo(
            string Name,
            IReadOnlyList<PassInfo<LoopPassArgument, LoopPassResult>> LoopPasses,
            IReadOnlyList<PassInfo<BodyPassArgument, IStatement>> FinalizationPasses,
            int DefaultMaxIterations)
            : this(
                Name, LoopPasses, FinalizationPasses, 
                GetMaxIterationsOption(Name), DefaultMaxIterations)
        {
            this.passName = Name;
            this.MaxIterationsOption = MaxIterationsOption;
            this.DefaultMaxIterations = DefaultMaxIterations;
            this.LoopPasses = LoopPasses;
            this.FinalizationPasses = FinalizationPasses;
        }

        private string passName;

        /// <summary>
        /// Gets the list of loop pass descriptions.
        /// </summary>
        public IReadOnlyList<PassInfo<LoopPassArgument, LoopPassResult>> LoopPasses { get; private set; }

        /// <summary>
        /// Gets the list of finalization pass descriptions.
        /// </summary>
        public IReadOnlyList<PassInfo<BodyPassArgument, IStatement>> FinalizationPasses { get; private set; }

        /// <summary>
        /// Gets the described pass' name.
        /// </summary>
        public override string Name { get { return passName; } }

        /// <summary>
        /// Gets the option that allows a user to set the 
        /// maximal number of iterations for this loop.
        /// </summary>
        public string MaxIterationsOption { get; private set; }

        /// <summary>
        /// Gets the default maximal number of iterations
        /// for the loop pass.
        /// </summary>
        public int DefaultMaxIterations { get; private set; }

        /// <summary>
        /// Gets the pass name tree for this pass info.
        /// </summary>
        public override NameTree NameTree 
        { 
            get 
            { 
                return new NameTree(
                    Name, 
                    LoopPasses
                    .Select(p => p.NameTree)
                    .Concat(FinalizationPasses
                        .Select(p => p.NameTree))
                    .ToArray()); 
            } 
        }

        /// <summary>
        /// Creates a max iterations option name from the given loop
        /// name.
        /// </summary>
        public static string GetMaxIterationsOption(string LoopName)
        {
            return "max-" + LoopName + "-iterations";
        }

        public override IPass<BodyPassArgument, IStatement> Instantiate(PassSelector Selector)
        {
            int maxIters = Selector.Options.GetOption<int>(MaxIterationsOption, DefaultMaxIterations);
            return new PassLoop(
                Selector.InstantiateActive(LoopPasses), 
                Selector.InstantiateActive(FinalizationPasses), 
                maxIters);
        }
    }
}

