using Flame.Compiler;
using Flame.Compiler.Visitors;
using Flame.Optimization;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using Flame.Front.Passes;
using Flame.Compiler.Expressions;
using Flame.Compiler.Variables;
using Flame.Compiler.Statements;

namespace Flame.Front.Target
{
    /// <summary>
    /// A node visitor that computes the relative cost of
    /// inlining a method, provided that its caller is
    /// a viable candidate for scalar replacement.
    /// </summary>
    public sealed class ScalarReplacementSizeVisitor : SizeVisitor
    {
        public ScalarReplacementSizeVisitor(Func<IType, bool> IsScalarReplacementCandidate)
            : base(true)
        {
            this.IsScalarReplacementCandidate = IsScalarReplacementCandidate;
        }

        /// <summary>
        /// Gets a value indicating whether the given type is a candidate for scalar replacement.
        /// </summary>
        /// <value><c>true</c> if the given type is a scalar replacement candidate; otherwise, <c>false</c>.</value>
        public Func<IType, bool> IsScalarReplacementCandidate { get; private set; }

        private bool IsLocalOrArgVariable(IExpression Expr)
        {
            var varExpr = Expr.GetEssentialExpression() as IVariableNode;
            var srcVar = varExpr.GetVariable();
            return srcVar is ThisVariable
                || srcVar is ArgumentVariable
                || srcVar is LocalVariableBase;
        }

        private static int ApproximateAccessCost(IType Type)
        {
            // Assume that loads to sub-word-size memory locations
            // are just as expensive as loading a word.
            return Math.Max(InliningPass.WordSize, InliningPass.ApproximateSize(Type)) / 2;
        }

        /// <summary>
        /// "Visits" an expression: an expression is taken as input and transformed another expression.
        /// </summary>
        public override IExpression Visit(IExpression Value)
        {
            if (Value is FieldGetExpression)
            {
                var fieldGet = (FieldGetExpression)Value;
                if (IsLocalOrArgVariable(fieldGet.Target)
                    && IsScalarReplacementCandidate(fieldGet.Field.DeclaringType))
                {
                    // If this function were to get inlined and the aggregate
                    // gets replaced by scalars, then the field can be kept
                    // in a register. Being optimistic here could result in some
                    // really good results.
                    Size -= ApproximateAccessCost(fieldGet.Field.FieldType);
                    return fieldGet;
                }
            }
            else if (Value is FieldGetPointerExpression)
            {
                var fieldPtr = (FieldGetPointerExpression)Value;
                if (IsLocalOrArgVariable(fieldPtr.Target)
                    && IsScalarReplacementCandidate(fieldPtr.Field.DeclaringType))
                {
                    // If this function were to get inlined and the aggregate
                    // gets replaced by scalars, then the field can be kept
                    // in a register. Being optimistic here could result in some
                    // really good results.
                    Size -= ApproximateAccessCost(fieldPtr.Field.FieldType);
                    return fieldPtr;
                }
            }
            return base.Visit(Value);
        }

        /// <summary>
        /// "Visits" a statement: an statement is taken as input and transformed another statement.
        /// </summary>
        public override IStatement Visit(IStatement Value)
        {
            if (Value is FieldSetStatement)
            {
                var fieldSet = (FieldSetStatement)Value;
                if (IsLocalOrArgVariable(fieldSet.Target)
                    && IsScalarReplacementCandidate(fieldSet.Field.DeclaringType))
                {
                    // If this function were to get inlined and the aggregate
                    // gets replaced by scalars, then the field can be kept
                    // in a register. Being optimistic here could result in some
                    // really good results.
                    Size -= ApproximateAccessCost(fieldSet.Field.FieldType);
                    // We do, however, have to consider the field's value.
                    Visit(fieldSet.Value);
                    return fieldSet;
                }
            }
            return base.Visit(Value);
        }
    }

    public class ScalarReplacementPass : ScalarReplacementPassBase
    {
        private ScalarReplacementPass()
        { }

        public static readonly ScalarReplacementPass Instance = new ScalarReplacementPass();

        public const string ScalarReplacementInlineToleranceOption = "scalarrepl-" + InliningPass.InlineToleranceOption;
        public const int DefaultScalarReplacementInlineTolerance = InliningPass.DefaultInlineTolerance + DefaultScalarReplacementTolerance;

        public const string ScalarReplacementToleranceOption = "scalarrepl-tolerance";
        // By default, don't replace aggregates that are more than sixteen words
        // (i.e. sixteen ints or eight longs) wide. On most architectures, this
        // boils down to 64 bytes.
        public const int DefaultScalarReplacementTolerance = 16 * InliningPass.WordSize;

        private IStatement GetMethodBody(BodyPassArgument Value, IMethod Method)
        {
            var result = Value.PassEnvironment.GetMethodBody(Method);

            if (result == null)
            {
                return null;
            }

            return CloningVisitor.Instance.Visit(result);
        }

        private static int GetInlineTolerance(ICompilerOptions Options)
        {
            if (Options.HasOption(ScalarReplacementInlineToleranceOption))
                return Options.GetOption<int>(
                    ScalarReplacementInlineToleranceOption,
                    DefaultScalarReplacementInlineTolerance);
            else if (Options.HasOption(InliningPass.InlineToleranceOption))
                return DefaultScalarReplacementTolerance + Options.GetOption<int>(
                    InliningPass.InlineToleranceOption,
                    InliningPass.DefaultInlineTolerance);
            else
                return DefaultScalarReplacementInlineTolerance;
        }

        public override Func<IType, bool> GetReplacementCriteria(BodyPassArgument Argument)
        {
            var log = Argument.PassEnvironment.Log;

            int tolerance = log.Options.GetOption<int>(
                ScalarReplacementToleranceOption, DefaultScalarReplacementTolerance);
            return ty => InliningPass.ApproximateSize(ty) <= tolerance;
        }

        public override Func<DissectedCall, bool> GetInliningCriteria(BodyPassArgument Argument)
        {
            var log = Argument.PassEnvironment.Log;

            int inlineTolerance = GetInlineTolerance(log.Options);
            var replCriteria = GetReplacementCriteria(Argument);
            Func<IType, bool> canRepl = ty => ty.GetIsValueType() && replCriteria(ty);
            return call => InliningPass.ShouldInline(
                Argument, call,
                body =>
                {
                    var visitor = new ScalarReplacementSizeVisitor(canRepl);
                    visitor.Visit(body);
                    return visitor.Size - inlineTolerance;
                },
                false);
        }
    }
}
