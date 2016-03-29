using Flame.Compiler;
using Flame.Compiler.Visitors;
using Flame.Optimization;
using Flame.Optimization.Variables;
using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using Flame.Front.Passes;
using Flame.Compiler.Variables;

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

        private static int RateArgument(IType ParameterType, IExpression Argument)
        {
            var argType = Argument.Type;

            // The bigger the size of the argument type,
            // the costlier the function call itself is.
            int argSize = ApproximateSize(argType);

            // This is interesting, because it may allow us to
            // replace indirect calls with direct calls.
            int inheritanceBoost = !argType.Equals(ParameterType) ? 4 : 0;

            // Constants may allow us to eliminate branches.
            int constantBoost = Argument.GetIsConstant() ? 4 : 0;

            // Delegates can be sometimes be replaced
            // with direct or virtual calls.
            int delegateBoost = ParameterType.GetIsDelegate() ? 4 : 0;

            // We also want to boost addresses to local variables.
			// These addresses can often be converted to direct variable access
            // once inlining has been performed.
            // Inlining things that involve local variables
            // also tends to help scalar replacement of aggregates
            // a great deal.
            var essentialExpr = Argument.GetEssentialExpression();
            int localBoost = 0;
            if (essentialExpr is IVariableNode)
            {
                var varNode = (IVariableNode)essentialExpr;
                var variable = varNode.GetVariable();
                if (variable is LocalVariableBase)
                {
                    if (varNode.Action == VariableNodeAction.AddressOf)
                    {
                        localBoost = ApproximateSize(variable.Type);
                    }
                }
            }

            return argSize + inheritanceBoost + constantBoost + delegateBoost + localBoost;
        }

        public static bool ShouldInline(BodyPassArgument Args, DissectedCall Call, Func<IStatement, int> ComputeCost, bool RespectAccess)
        {
            if (Call.Method.GetIsVirtual() || (Call.Method.IsConstructor && !Call.Method.DeclaringType.GetIsValueType()))
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

		public override int GetMaxRecursion(BodyPassArgument Argument)
		{
			var log = Argument.PassEnvironment.Log;
			return log.Options.GetOption<int>("max-inline-recursion", 3);
		}

		public override Func<IStatement, IStatement> GetBodyOptimizer(BodyPassArgument Argument)
		{
			var emptyLog = new EmptyCompilerLog(Argument.PassEnvironment.Log.Options);
			var passManager = new PassManager(PassExtensions.SSAPassManager);
			var optSuite = passManager.CreateSuite(emptyLog);
			var newArgs = new BodyPassArgument(
				new DerivedBodyPassEnvironment(Argument.PassEnvironment, emptyLog), Argument.Metadata,
				Argument.DeclaringMethod, null);
			return body => optSuite.MethodPass.Apply(new BodyPassArgument(newArgs, body));
		}
    }
}
