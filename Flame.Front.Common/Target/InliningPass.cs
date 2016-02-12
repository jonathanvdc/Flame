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
	}

    public class InliningPass : InliningPassBase
    {
        private InliningPass()
        { }

		public static readonly InliningPass Instance = new InliningPass();

		private const int WordSize = 4;

        private static int ApproximateSize(IType Type)
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

            int inheritanceBoost = !argType.Equals(ParameterType) ? 4 : 0;  // This is interesting, because it may allow us to
                                                                            // replace indirect calls with direct calls

            int constantBoost = Argument.IsConstant ? 4 : 0;                // Constants may allow us to eliminate branches

            int delegateBoost = ParameterType.GetIsDelegate() ? 4 : 0;     // Delegates can be sometimes be replaced with direct or virtual calls.

            return ApproximateSize(argType) + inheritanceBoost + constantBoost + delegateBoost;
        }

        public bool ShouldInline(BodyPassArgument Args, DissectedCall Call, int Tolerance)
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
            if (body == null || !AccessChecker.CanAccess(thisType, body))
            {
                return false;
            }

            int pro = Call.ThisValue != null ? RateArgument(Call.Method.DeclaringType, Call.ThisValue) : 0;
            foreach (var item in Call.Method.Parameters.Zip(Call.Arguments, Tuple.Create))
            {
                pro += RateArgument(item.Item1.ParameterType, item.Item2);
            }

            int con = SizeVisitor.ApproximateSize(body, true, 2);

            return con - pro < Tolerance;
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
			int inlineTolerance = log.Options.GetOption<int>("inline-tolerance", 0);
			return call => ShouldInline(Argument, call, inlineTolerance);
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
