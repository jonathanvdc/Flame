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

namespace Flame.Front.Target
{
    public class InliningPass : IPass<BodyPassArgument, IStatement>
    {
        private InliningPass()
        {

        }

        static InliningPass()
        {
            Instance = new InliningPass();
        }

        public static InliningPass Instance { get; private set; }

        /// <summary>
        /// The inlining pass' name.
        /// </summary>
        public const string InliningPassName = "inline";

        /// <summary>
        /// The name of the option that enables inlining remarks.
        /// </summary>
        public static readonly string InliningRemarksOption = Flags.Instance.GetRemarkOptionName(InliningPassName);

        private static int ApproximateSize(IType Type)
        {
            int primSize = Type.GetPrimitiveSize();
            if (primSize > 0)
            {
                return primSize;
            }

            if (Type.get_IsReferenceType() || Type.get_IsPointer() || Type.get_IsArray())
            {
                return 4;
            }

            if (Type.get_IsVector())
            {
                return ApproximateSize(Type.GetEnumerableElementType()) * Type.AsContainerType().AsVectorType().Dimensions.Aggregate(1, (aggr, val) => aggr * val);
            }

            return Type.Fields.Aggregate(0, (aggr, field) => aggr + ApproximateSize(field.FieldType));
        }

        private static int RateArgument(IType ParameterType, IExpression Argument)
        {
            var argType = Argument.Type;

            int inheritanceBoost = !argType.Equals(ParameterType) ? 4 : 0;  // This is interesting, because it may allow us to
                                                                            // replace indirect calls with direct calls

            int constantBoost = Argument.IsConstant ? 4 : 0;                // Constants may allow us to eliminate branches

            int delegateBoost = ParameterType.get_IsDelegate() ? 4 : 0;     // Delegates can be sometimes be replaced with direct or virtual calls.

            return ApproximateSize(argType) + inheritanceBoost + constantBoost + delegateBoost;
        }

        public bool ShouldInline(BodyPassArgument Args, DissectedCall Call, int Tolerance)
        {
            if (Call.Method.get_IsVirtual() || (Call.Method.IsConstructor && !Call.Method.DeclaringType.get_IsValueType()))
            {
                return false;
            }

            var body = Args.PassEnvironment.GetMethodBody(Call.Method);
            var thisType = Args.DeclaringType;
            if (thisType.get_IsGeneric() && thisType.get_IsGenericDeclaration())
            {
                thisType = thisType.MakeGenericType(thisType.GenericParameters);
            }
            if (body == null || !AccessChecker.CanAccess(thisType, body))
            {
                return false;
            }

            int pro = Call.ThisValue != null ? RateArgument(Call.Method.DeclaringType, Call.ThisValue) : 0;
            foreach (var item in Call.Method.GetParameters().Zip(Call.Arguments, Tuple.Create))
            {
                pro += RateArgument(item.Item1.ParameterType, item.Item2);
            }

            int con = SizeVisitor.ApproximateSize(body, true, 2);

            return con - pro < Tolerance;
        }

        private IStatement OptimizeSimple(IStatement Value)
        {
            return Value.Optimize();
        }

        private IStatement OptimizeAdvanced(IStatement Value)
        {
            return DefinitionPropagationPass.Instance.Apply(Value.Optimize()).Optimize();
        }

        public IStatement Apply(BodyPassArgument Value)
        {
            var log = Value.PassEnvironment.Log;
            int maxRecursion = log.Options.GetOption<int>("max-inline-recursion", 3);
            int inlineTolerance = log.Options.GetOption<int>("inline-tolerance", 0);
            bool propInline = log.Options.GetOption<bool>("inline-propagate-locals", true);

            var inliner = new InliningVisitor(Value.DeclaringMethod, call => ShouldInline(Value, call, inlineTolerance),
                                              Value.PassEnvironment.GetMethodBody, 
                                              propInline ? new Func<IStatement, IStatement>(OptimizeAdvanced) 
                                                         : new Func<IStatement, IStatement>(OptimizeSimple),
                                              maxRecursion);
            var result = inliner.Visit(Value.Body);
            if (inliner.HasInlined)
            {
                result = result.Optimize();

                if (inliner.InlinedCallLocations.Any() && log.Options.GetOption<bool>(InliningRemarksOption, false))
                {
                    foreach (var item in inliner.InlinedCallLocations)
                    {
                        log.LogMessage(new LogEntry("Pass remark", new IMarkupNode[]
                        {
                            new MarkupNode(
                                NodeConstants.TextNodeType, 
                                "Inlined call to '" + item.Key.Method.Name + "' into '" + Value.DeclaringMethod.Name + "'. "),
                            Flags.Instance.CreateCauseNode(InliningRemarksOption)
                        }, item.Value));
                    }
                }
            }
            return result;
        }
    }
}
