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
    public class InitializationCountPass : IPass<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>
    {
        public InitializationCountPass(ICompilerLog Log)
        {
            this.Log = Log;
        }

        public ICompilerLog Log { get; private set; }

        /// <summary>
        /// A warning name for uninitialized values.
        /// </summary>
        public const string UninitializedWarningName = "uninitialized";

        /// <summary>
        /// A warning name for potentially uninitialized values.
        /// </summary>
        public const string MaybeUninitializedWarningName = "maybe-uninitialized";

        /// <summary>
        /// A warning name for multiply initialized values.
        /// </summary>
        public const string MultipleInitializationWarningName = "multiple-initialization";

        /// <summary>
        /// A warning name for potentially multiply initialized values.
        /// </summary>
        public const string MaybeMultipleInitializationWarningName = "maybe-multiple-initialization";

        /// <summary>
        /// Gets a boolean value that tells if this pass is useful for the given log,
        /// i.e. not all of its warnings have been suppressed.
        /// </summary>
        /// <param name="Log"></param>
        /// <returns></returns>
        public static bool IsUseful(ICompilerLog Log)
        {
            return Log.UseDefaultWarnings(UninitializedWarningName) || Log.UseDefaultWarnings(MultipleInitializationWarningName) ||
                   Log.UsePedanticWarnings(MaybeUninitializedWarningName) || Log.UsePedanticWarnings(MaybeMultipleInitializationWarningName);
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
            var newContents = Visitor.MatchLocations.Aggregate(Entry.Contents, 
                (state, item) => RedefinitionHelpers.Instance.AppendDiagnosticsRemark(state, "Initialization: ", item));
            return new LogEntry(Entry.Name, newContents);
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
                    var msg = new LogEntry("Instance uninitialized",
                                           "The constructed instance is never initialized by this constructor. " +
                                           Warnings.Instance.GetWarningNameMessage(UninitializedWarningName),
                                           Target.GetSourceLocation());
                    Log.LogWarning(AppendInitializationLocations(msg, Visitor));
                }
                else if (Log.UsePedanticWarnings(MaybeUninitializedWarningName))
                {
                    var msg = new LogEntry("Instance possibly uninitialized",
                                           "Some control flow paths may not initialize the constructed instance. " +
                                           Warnings.Instance.GetWarningNameMessage(MaybeUninitializedWarningName),
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
                if (Visitor.CurrentFlow.Min > 1 && Log.UseDefaultWarnings(MultipleInitializationWarningName))
                {
                    var msg = new LogEntry("Instance initialized more than once",
                                           "The constructed instance is initialized more than once by this constructor. " +
                                           Warnings.Instance.GetWarningNameMessage(MultipleInitializationWarningName),
                                           Target.GetSourceLocation());
                    Log.LogWarning(AppendInitializationLocations(msg, Visitor));
                }
                else if (Log.UsePedanticWarnings(MaybeMultipleInitializationWarningName))
                {
                    var msg = new LogEntry("Instance possibly initialized more than once",
                                           "The constructed instance may be initialized more than once in some control flow paths in this constructor. " +
                                           Warnings.Instance.GetWarningNameMessage(MaybeMultipleInitializationWarningName),
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

        public Tuple<IStatement, IMethod> Apply(Tuple<IStatement, IMethod> Value)
        {
            if (Value.Item2.IsConstructor && !Value.Item2.IsStatic)
            {
                var visitor = InitializationCountHelpers.CreateVisitor();
                visitor.Visit(Value.Item1);
                if (!LogUninitialized(Log, Value.Item2, visitor))
                {
                    LogMultipleInitialization(Log, Value.Item2, visitor);
                }
            }
            return Value;
        }
    }
}
