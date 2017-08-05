using System;
using System.Collections.Generic;
using System.Linq;
using Flame.Compiler.Visitors;
using Flame.Compiler;
using Flame.Recompilation;
using Flame.Front.Target;

namespace Flame.Front.Passes
{
    using AnalysisPassInfo = PassInfo<Tuple<IStatement, IMethod, ICompilerLog>, IStatement>;
    using MethodPassInfo = PassInfo<BodyPassArgument, IStatement>;
    using SignaturePassInfo = PassInfo<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>;
    using StatementPassInfo = PassInfo<IStatement, IStatement>;
    using RootPassInfo = PassInfo<BodyPassArgument, IEnumerable<IMember>>;
    using IRootPass = IPass<BodyPassArgument, IEnumerable<IMember>>;
    using ISignaturePass = IPass<MemberSignaturePassArgument<IMember>, MemberSignaturePassResult>;

    /// <summary>
    /// A class of objects that manage a sequence of passes and their associated conditions.
    /// </summary>
    public class PassManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Flame.Front.Passes.PassManager"/> class.
        /// </summary>
        public PassManager()
        {
            this.MethodPasses = new List<MethodPassInfo>();
            this.LoweringPasses = new List<MethodPassInfo>();
            this.RootPasses = new List<RootPassInfo>();
            this.SignaturePasses = new List<SignaturePassInfo>();
            this.PassConditions = new List<PassCondition>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Flame.Front.Passes.PassManager"/> class.
        /// </summary>
        /// <param name="Preferences">A set of pass preferences.</param>
        public PassManager(PassPreferences Preferences)
        {
            this.MethodPasses = new List<MethodPassInfo>(Preferences.MethodPasses);
            this.LoweringPasses = new List<MethodPassInfo>(Preferences.LoweringPasses);
            this.RootPasses = new List<RootPassInfo>(Preferences.RootPasses);
            this.SignaturePasses = new List<SignaturePassInfo>(Preferences.SignaturePasses);
            this.PassConditions = new List<PassCondition>(Preferences.Conditions);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Flame.Front.Passes.PassManager"/> class.
        /// </summary>
        /// <param name="Other">The pass manager to copy.</param>
        public PassManager(PassManager Other)
        {
            this.MethodPasses = new List<MethodPassInfo>(Other.MethodPasses);
            this.LoweringPasses = new List<MethodPassInfo>(Other.LoweringPasses);
            this.RootPasses = new List<RootPassInfo>(Other.RootPasses);
            this.SignaturePasses = new List<SignaturePassInfo>(Other.SignaturePasses);
            this.PassConditions = new List<PassCondition>(Other.PassConditions);
        }

        /// <summary>
        /// Gets the list of method passes, which analyze and/or
        /// optimize method bodies.
        /// </summary>
        public List<MethodPassInfo> MethodPasses { get; private set; }

        /// <summary>
        /// Gets a list of machine lowering passes, which perform
        /// lowering and target-specific optimizations.
        /// </summary>
        public List<MethodPassInfo> LoweringPasses { get; private set; }

        /// <summary>
        /// Gets a list of root passes.
        /// </summary>
        public List<RootPassInfo> RootPasses { get; private set; }

        /// <summary>
        /// Gets a list of signature passes.
        /// </summary>
        public List<SignaturePassInfo> SignaturePasses { get; private set; }

        /// <summary>
        /// Gets a list of pass conditions.
        /// </summary>
        public List<PassCondition> PassConditions { get; private set; }

        /// <summary>
        /// Registers the given analysis pass.
        /// </summary>
        /// <param name="Pass"></param>
        public void RegisterMethodPass(PassInfo<Tuple<IStatement, IMethod, ICompilerLog>, IStatement> Pass)
        {
            RegisterMethodPass(BodyAnalysisPass.ToBodyPass(Pass));
        }

        /// <summary>
        /// Registers the given method body pass.
        /// </summary>
        /// <param name="Pass"></param>
        public void RegisterMethodPass(MethodPassInfo Pass)
        {
            MethodPasses.Add(Pass);
        }

        /// <summary>
        /// Registers the given statement pass.
        /// </summary>
        /// <param name="Pass"></param>
        public void RegisterMethodPass(StatementPassInfo Pass)
        {
            RegisterMethodPass(BodyStatementPass.ToBodyPass(Pass));
        }

        /// <summary>
        /// Registers the given analysis pass as a machine lowering pass.
        /// </summary>
        /// <param name="Pass"></param>
        public void RegisterLoweringPass(PassInfo<Tuple<IStatement, IMethod, ICompilerLog>, IStatement> Pass)
        {
            RegisterLoweringPass(BodyAnalysisPass.ToBodyPass(Pass));
        }

        /// <summary>
        /// Registers the given method body pass as a machine lowering pass.
        /// </summary>
        /// <param name="Pass"></param>
        public void RegisterLoweringPass(MethodPassInfo Pass)
        {
            LoweringPasses.Add(Pass);
        }

        /// <summary>
        /// Registers the given statement pass as a machine lowering pass.
        /// </summary>
        /// <param name="Pass"></param>
        public void RegisterLoweringPass(StatementPassInfo Pass)
        {
            RegisterLoweringPass(BodyStatementPass.ToBodyPass(Pass));
        }

        /// <summary>
        /// Registers the given root pass.
        /// </summary>
        /// <param name="Pass"></param>
        public void RegisterRootPass(RootPassInfo Pass)
        {
            RootPasses.Add(Pass);
        }

        /// <summary>
        /// Registers the given signature pass.
        /// </summary>
        /// <param name="Pass"></param>
        public void RegisterSignaturePass(SignaturePassInfo Pass)
        {
            SignaturePasses.Add(Pass);
        }

        /// <summary>
        /// Registers a sufficient condition for a
        /// pass to be run.
        /// </summary>
        /// <param name="Condition"></param>
        public void RegisterPassCondition(PassCondition Condition)
        {
            PassConditions.Add(Condition);
        }

        /// <summary>
        /// Registers a sufficient condition for the
        /// pass with the given name to be run.
        /// </summary>
        /// <param name="PassName"></param>
        /// <param name="Condition"></param>
        public void RegisterPassCondition(string PassName, Func<OptimizationInfo, bool> Condition)
        {
            RegisterPassCondition(new PassCondition(PassName, Condition));
        }

        /// <summary>
        /// Prepend the specified pass preferences to this pass manager.
        /// </summary>
        public void Prepend(PassPreferences Preferences)
        {
            this.MethodPasses.InsertRange(0, Preferences.MethodPasses);
            this.LoweringPasses.InsertRange(0, Preferences.LoweringPasses);
            this.RootPasses.InsertRange(0, Preferences.RootPasses);
            this.SignaturePasses.InsertRange(0, Preferences.SignaturePasses);
            this.PassConditions.InsertRange(0, Preferences.Conditions);
        }

        /// <summary>
        /// Append the specified pass preferences to this pass manager.
        /// </summary>
        public void Append(PassPreferences Preferences)
        {
            this.MethodPasses.AddRange(Preferences.MethodPasses);
            this.LoweringPasses.AddRange(Preferences.LoweringPasses);
            this.RootPasses.AddRange(Preferences.RootPasses);
            this.SignaturePasses.AddRange(Preferences.SignaturePasses);
            this.PassConditions.AddRange(Preferences.Conditions);
        }

        /// <summary>
        /// Converts this pass manager to a set of pass preferences.
        /// </summary>
        public PassPreferences ToPreferences()
        {
            return new PassPreferences(
                PassConditions, MethodPasses, LoweringPasses,
                RootPasses, SignaturePasses);
        }

        /// <summary>
        /// Creates an aggregate root pass from the given sequence
        /// of root passes.
        /// </summary>
        /// <param name="RootPasses"></param>
        /// <returns></returns>
        private IRootPass Aggregate(IEnumerable<IRootPass> RootPasses)
        {
            return RootPasses.Aggregate<IRootPass, IRootPass>(EmptyRootPass.Instance, (result, item) => new AggregateRootPass(result, item));
        }

        /// <summary>
        /// Creates an aggregate signature pass from the given sequence
        /// of signature passes.
        /// </summary>
        /// <param name="RootPasses"></param>
        /// <returns></returns>
        private ISignaturePass Aggregate(IEnumerable<ISignaturePass> RootPasses)
        {
            return RootPasses.Aggregate<ISignaturePass, ISignaturePass>(EmptyMemberSignaturePass<IMember>.Instance,
                (result, item) => new AggregateMemberSignaturePass<IMember>(result, item));
        }

        /// <summary>
        /// Creates a pass selector from the given optimization info.
        /// </summary>
        public PassSelector CreateSelector(OptimizationInfo OptInfo)
        {
            return new PassSelector(OptInfo, PassConditions);
        }

        /// <summary>
        /// Gets the names of all passes that are selected by the
        /// given optimization info and pass preferences. The
        /// results are returned as a dictionary that maps pass types
        /// to a sequence of selected pass names.
        /// </summary>
        /// <param name="Log"></param>
        /// <param name="Preferences"></param>
        /// <returns></returns>
        public IReadOnlyDictionary<string, IEnumerable<NameTree>> GetSelectedPassNames(OptimizationInfo OptInfo)
        {
            var selector = CreateSelector(OptInfo);

            return new Dictionary<string, IEnumerable<NameTree>>()
            {
                { "Signature", SignaturePasses.Select(p => selector.SelectActive(p.NameTree)).Where(x => x != null).ToArray() },
                { "Body", MethodPasses.Select(p => selector.SelectActive(p.NameTree)).Where(x => x != null).ToArray() },
                { "Lowering", LoweringPasses.Select(p => selector.SelectActive(p.NameTree)).Where(x => x != null).ToArray() },
                { "Root", RootPasses.Select(p => selector.SelectActive(p.NameTree)).Where(x => x != null).ToArray() }
            };
        }


        /// <summary>
        /// Gets the names of all passes that are selected by the
        /// given compiler log. The
        /// results are returned as a dictionary that maps pass types
        /// to a sequence of selected pass names.
        /// </summary>
        /// <param name="Log"></param>
        /// <returns></returns>
        public IReadOnlyDictionary<string, IEnumerable<NameTree>> GetSelectedPassNames(ICompilerLog Log)
        {
            return GetSelectedPassNames(new OptimizationInfo(Log));
        }

        /// <summary>
        /// Creates a pass suite from the given compiler log.
        /// </summary>
        /// <param name="Log"></param>
        /// <returns></returns>
        public PassSuite CreateSuite(ICompilerLog Log)
        {
            var optInfo = new OptimizationInfo(Log);
            var selector = CreateSelector(optInfo);

            // Select passes by relying on the optimization info
            // and pass preferences.
            var selectedMethodPasses = selector.InstantiateActive(MethodPasses);
            var selectedLoweringPasses = selector.InstantiateActive(LoweringPasses);
            var selectedRootPasses = selector.InstantiateActive(RootPasses);
            var selectedSigPasses = selector.InstantiateActive(SignaturePasses);

            return new PassSuite(
                selectedMethodPasses.Aggregate(),
                selectedLoweringPasses.Aggregate(),
                Aggregate(selectedRootPasses),
                Aggregate(selectedSigPasses));
        }
    }
}

