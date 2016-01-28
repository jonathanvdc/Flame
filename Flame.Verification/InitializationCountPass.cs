using Flame.Analysis;
using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Compiler.Visitors;
using Flame.Syntax;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Verification
{
    /// <summary>
    /// A pass that checks initialization counts and reports irregularities.
    /// </summary>
    public class InitializationCountPass : IPass<Tuple<IStatement, IMethod, ICompilerLog>, IStatement>
    {
        private InitializationCountPass()
        { }

        static InitializationCountPass()
        {
            Instance = new InitializationCountPass();
        }

        public static InitializationCountPass Instance { get; private set; }

        /// <summary>
        /// A warning for uninitialized values.
        /// </summary>
        public static readonly WarningDescription UninitializedWarning = new WarningDescription("uninitialized", Warnings.Instance.All);

        /// <summary>
        /// A warning for potentially uninitialized values.
        /// </summary>
        public static readonly WarningDescription MaybeUninitializedWarning = new WarningDescription("maybe-uninitialized", Warnings.Instance.Extra);

        /// <summary>
        /// A warning for multiply initialized values.
        /// </summary>
        public static readonly WarningDescription MultipleInitializationWarning = new WarningDescription("multiple-initialization", Warnings.Instance.All);

        /// <summary>
        /// A warning for potentially multiply initialized values.
        /// </summary>
        public static readonly WarningDescription MaybeMultipleInitializationWarning = new WarningDescription("maybe-multiple-initialization", Warnings.Instance.Extra);

        /// <summary>
        /// Gets a boolean value that tells if this pass is useful for the given log,
        /// i.e. not all of its warnings have been suppressed.
        /// </summary>
        /// <param name="Log"></param>
        /// <returns></returns>
        public static bool IsUseful(ICompilerLog Log)
        {
            return UninitializedWarning.UseWarning(Log.Options) ||
                   MultipleInitializationWarning.UseWarning(Log.Options) ||
                   MaybeUninitializedWarning.UseWarning(Log.Options) ||
                   MaybeMultipleInitializationWarning.UseWarning(Log.Options);
        }

        /// <summary>
        /// Appends the node count visitor's locations to the given entry as
        /// neutral diagnostic remarks with the given title.
        /// </summary>
        /// <param name="Entry"></param>
        /// <param name="Visitor"></param>
        /// <returns></returns>
        public static LogEntry AppendNeutralLocations(LogEntry Entry, NodeCountVisitor Visitor, string Title)
        {
            var newContents = Visitor.MatchLocations.Aggregate(Entry.Contents,
                (state, item) => RedefinitionHelpers.Instance.AppendDiagnosticsRemark(state, Title, item));
            return new LogEntry(Entry.Name, newContents);
        }

        /// <summary>
        /// Appends the node count visitor's locations to the given entry as
        /// initialization diagnostic remarks.
        /// </summary>
        /// <param name="Entry"></param>
        /// <param name="Visitor"></param>
        /// <returns></returns>
        public static LogEntry AppendInitializationLocations(LogEntry Entry, NodeCountVisitor Visitor)
        {
            return AppendNeutralLocations(Entry, Visitor, "Initialization: ");
        }

        /// <summary>
        /// Checks if the given visitor's current flow indicates possibly uninitialized flow,
        /// and logs it if that is the case. A boolean is returned that tells if the control flow
        /// is possibly uninitialized.
        /// </summary>
        /// <param name="Log"></param>
        /// <param name="Target"></param>
        /// <param name="Visitor"></param>
        /// <returns></returns>
        public static bool LogUninitialized(ICompilerLog Log, IMethod Target, NodeCountVisitor Visitor)
        {
            if (Visitor.CurrentFlow.Min == 0)
            {
                if (Visitor.CurrentFlow.Max == 0)
                {
                    if (UninitializedWarning.UseWarning(Log.Options))
                    {
                        var msg = new LogEntry(
                            "instance uninitialized",
                            UninitializedWarning.CreateMessage(
                                "the constructed instance is never initialized by this constructor. "),
                            Target.GetSourceLocation());
                        Log.LogWarning(AppendInitializationLocations(msg, Visitor));
                    }
                }
                else if (MaybeUninitializedWarning.UseWarning(Log.Options))
                {
                    var msg = new LogEntry(
                        "instance possibly uninitialized",
                        MaybeUninitializedWarning.CreateMessage(
                        "some control flow paths may not initialize the constructed instance. "),
                        Target.GetSourceLocation());
                    Log.LogWarning(AppendInitializationLocations(msg, Visitor));
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the given visitor's current flow indicates possibly multiply initialized flow,
        /// and logs it if that is the case. A boolean is returned that tells if the control flow
        /// is possibly multiply initialized.
        /// </summary>
        /// <param name="Log"></param>
        /// <param name="Target"></param>
        /// <param name="Visitor"></param>
        /// <returns></returns>
        public static bool LogMultipleInitialization(ICompilerLog Log, IMethod Target, NodeCountVisitor Visitor)
        {
            if (Visitor.CurrentFlow.Max > 1)
            {
                if (Visitor.CurrentFlow.Min > 1)
                {
                    if (MultipleInitializationWarning.UseWarning(Log.Options))
	                {
                        var msg = new LogEntry(
                            "instance initialized more than once",
                            MultipleInitializationWarning.CreateMessage(
                                "the constructed instance is initialized more than once by this constructor. "),
                            Target.GetSourceLocation());
                        Log.LogWarning(AppendInitializationLocations(msg, Visitor));
	                }
                }
                else if (MaybeMultipleInitializationWarning.UseWarning(Log.Options))
                {
                    var msg = new LogEntry(
                        "instance possibly initialized more than once",
                        MaybeMultipleInitializationWarning.CreateMessage(
                            "the constructed instance may be initialized more than once in some control flow paths in this constructor. "),
                        Target.GetSourceLocation());
                    Log.LogWarning(AppendInitializationLocations(msg, Visitor));
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Tests if the given type can and must be initialized.
        /// </summary>
        public static bool IsInitializableType(IType Type)
        {
            return Type != null && (Type.GetIsValueType() || Type.GetParent() != null);
        }

        public IStatement Apply(Tuple<IStatement, IMethod, ICompilerLog> Value)
        {
            var log = Value.Item3;
            if (Value.Item2.IsConstructor && !Value.Item2.IsStatic &&
                IsInitializableType(Value.Item2.DeclaringType))
            {
                var visitor = InitializationCountHelpers.CreateVisitor();
                visitor.Visit(Value.Item1);
                if (!LogUninitialized(log, Value.Item2, visitor))
                {
                    LogMultipleInitialization(log, Value.Item2, visitor);
                }
            }
            return Value.Item1;
        }
    }
}
