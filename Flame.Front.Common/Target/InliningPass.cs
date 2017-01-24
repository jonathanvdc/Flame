using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using Flame.Front.Passes;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Variables;
using Flame.Compiler.Visitors;
using Flame.Optimization;

namespace Flame.Front.Target
{
    /// <summary>
    /// A body pass that is derived from another underlying
    /// body pass environment, but comes with its own log.
    /// </summary>
    public class DerivedBodyPassEnvironment : IBodyPassEnvironment
    {
        public DerivedBodyPassEnvironment(
            IBodyPassEnvironment BaseEnvironment, ICompilerLog Log)
        {
            this.BaseEnvironment = BaseEnvironment;
            this.Log = Log;
        }

        /// <summary>
        /// Gets the body pass environment this body pass environment
        /// is based on.
        /// </summary>
        /// <value>The base environment.</value>
        public IBodyPassEnvironment BaseEnvironment { get; private set; }

        /// <summary>
        /// Gets the body pass' log.
        /// </summary>
        public ICompilerLog Log { get; private set; }

        /// <summary>
        /// Gets the body pass' environment.
        /// </summary>
        public IEnvironment Environment { get { return BaseEnvironment.Environment; } }

        /// <summary>
        /// Gets the method body for the given method.
        /// </summary>
        /// <returns>The method body.</returns>
        public IStatement GetMethodBody(IMethod Method)
        {
            return BaseEnvironment.GetMethodBody(Method);
        }

        /// <summary>
        /// Checks if the given type can be extended with additional members.
        /// </summary>
        /// <returns><c>true</c> if the specified type can be extended; otherwise, <c>false</c>.</returns>
        public bool CanExtend(IType Type)
        {
            return BaseEnvironment.CanExtend(Type);
        }
    }

    public class InliningPass : InliningPassBase
    {
        private InliningPass()
        { }

        public static readonly InliningPass Instance = new InliningPass();

        public const string InlineToleranceOption = "inline-tolerance";
        public const int DefaultInlineTolerance = 0;

        /// <summary>
        /// The word size that is used for inlining heuristics.
        /// </summary>
        public const int WordSize = 4;

        /// <summary>
        /// Heuristically tries to approximate the given type's size.
        /// </summary>
        public static int ApproximateSize(IType Type)
        {
            int primSize = Type.GetPrimitiveSize();
            if (primSize > 0)
            {
                return primSize;
            }
            else if (Type.GetIsReferenceType() || Type.GetIsPointer() || Type.GetIsArray())
            {
                return WordSize;
            }
            else if (Type.GetIsVector())
            {
                return ApproximateSize(Type.GetEnumerableElementType()) * Type.AsContainerType().AsVectorType().Dimensions.Aggregate(1, (aggr, val) => aggr * val);
            }
            else if (Type.GetIsEnum())
            {
                var parentTy = Type.GetParent();
                if (parentTy == null)
                    return WordSize;
                else
                    return ApproximateSize(parentTy);
            }
            else if (Type.GetIsValueType())
            {
                return Type.Fields.Where(item => !item.IsStatic).Aggregate(0, (aggr, field) => aggr + ApproximateSize(field.FieldType));
            }
            else
            {
                // We have absolutely no idea of what this thing is.
                // Assume that it's two words wide.
                return 2 * WordSize;
            }
        }

        /// <summary>
        /// The boost obtained from having a parameter that may get rid of
        /// a level of indirection.
        /// </summary>
        private const int IndirectParameterBoost = 2 * WordSize;

        private static bool IsGetMethodExpression(IExpression Expression)
        {
            return Expression is GetMethodExpression
                || Expression is GetExtensionMethodExpression;
        }

        private static bool IsGetDirectMethodExpression(IExpression Expression)
        {
            var getMethodExpr = Expression as GetMethodExpression;
            if (getMethodExpr == null)
                return Expression is GetExtensionMethodExpression;
            else
                return getMethodExpr.Op.Equals(Operator.GetDelegate);
        }

        private static int RateArgument(IType ParameterType, IExpression Argument)
        {
            var essentialExpr = ConversionExpression.Instance.GetRawValueExpression(
                Argument.GetEssentialExpression());

            var argType = essentialExpr.Type;

            // The bigger the size of the argument type,
            // the costlier the function call itself is.
            int rating = ApproximateSize(argType);

            // This is interesting, because it may allow us to
            // replace indirect calls with direct calls.
            if (!argType.Equals(ParameterType))
            {
                rating += IndirectParameterBoost;
                if (!argType.GetIsVirtual())
                {
                    // Bingo. A `sealed` class can always have all of its
                    // methods inlined, so this is quite useful to inline.
                    rating += IndirectParameterBoost;
                }
            }

            // Constants may allow us to eliminate branches.
            rating += essentialExpr.GetIsConstant() ? IndirectParameterBoost : 0;

            // We want to boost addresses to local variables.
            // These addresses can often be converted to direct variable access
            // once inlining has been performed.
            // Inlining things that involve local variables
            // also tends to help scalar replacement of aggregates
            // a great deal.
            if (essentialExpr is IVariableNode)
            {
                var varNode = (IVariableNode)essentialExpr;
                var variable = varNode.GetVariable();
                if (variable is LocalVariableBase)
                {
                    if (varNode.Action == VariableNodeAction.AddressOf)
                    {
                        rating += ApproximateSize(variable.Type);
                    }
                }
            }
            // Delegate calls can often be replaced with more direct calls
            // when the callee is inlined. We should make this
            else if (IsGetMethodExpression(essentialExpr))
            {
                // Inlining may remove a level of indirection, so we should
                // encourage it.
                rating += IndirectParameterBoost;
                if (IsGetDirectMethodExpression(essentialExpr))
                {
                    // Inlining may remove a level of indirection, _and_ it may
                    // cause further inlining, so we should boost it even more.
                    rating += IndirectParameterBoost;
                }
            }

            return rating;
        }

        public static bool ShouldInline(BodyPassArgument Args, DissectedCall Call, Func<IStatement, int> ComputeCost, bool RespectAccess)
        {
            if (Call.IsVirtual || (Call.Method.IsConstructor && !Call.Method.DeclaringType.GetIsValueType()))
            {
                return false;
            }

            var body = Args.PassEnvironment.GetMethodBody(Call.Method);
            var thisType = Args.DeclaringType;
            if (thisType.GetIsGeneric() && thisType.GetIsGenericDeclaration())
            {
                thisType = thisType.MakeGenericType(thisType.GenericParameters);
            }
            if (body == null || (RespectAccess && !AccessChecker.CanAccess(thisType, body)))
            {
                return false;
            }

            int pro = Call.ThisValue != null ? RateArgument(Call.Method.DeclaringType, Call.ThisValue) : 0;
            pro += ApproximateSize(Call.Method.ReturnType);
            foreach (var item in Call.Method.Parameters.Zip(Call.Arguments, Tuple.Create))
            {
                pro += RateArgument(item.Item1.ParameterType, item.Item2);
            }

            int con = ComputeCost(body);

            return con - pro < 0;
        }

        public static bool ShouldInline(BodyPassArgument Args, DissectedCall Call, int Tolerance, bool RespectAccess)
        {
            return ShouldInline(Args, Call, body => SizeVisitor.ApproximateSize(body) - Tolerance, RespectAccess);
        }

        private IStatement GetMethodBody(BodyPassArgument Value, IMethod Method)
        {
            var result = Value.PassEnvironment.GetMethodBody(Method);

            if (result == null)
            {
                return null;
            }

            return CloningVisitor.Instance.Visit(result);
        }

        public override Func<DissectedCall, bool> GetInliningCriteria(BodyPassArgument Argument)
        {
            var log = Argument.PassEnvironment.Log;
            int inlineTolerance = log.Options.GetOption<int>(InlineToleranceOption, DefaultInlineTolerance);
            return call => ShouldInline(Argument, call, inlineTolerance, true);
        }
    }
}
