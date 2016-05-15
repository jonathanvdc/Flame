using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.ExpressionTrees.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees
{
    public class ExpressionMethod : DescribedMethod, IMethodBuilder, IInvocableMethod
    {
        public ExpressionMethod(string Name, IType DeclaringType, IType ReturnType, bool IsStatic)
            : base(Name, DeclaringType, ReturnType, IsStatic)
        {
            this.parameters = new List<ParameterExpression>();
            if (!IsStatic)
            {
                this.parameters.Add(Expression.Parameter(typeof(IBoundObject), "this"));
            }
            this.CodeGenerator = new ExpressionCodeGenerator(this);
        }

        public LambdaExpression Body { get; private set; }
        public ExpressionCodeGenerator CodeGenerator { get; private set; }

        private Lazy<Delegate> deleg;

        public Type ExpressionReturnType
        {
            get
            {
                return ExpressionTypeConverter.Instance.Convert(ReturnType);
            }
        }

        private List<ParameterExpression> parameters;
        public IReadOnlyList<ParameterExpression> ExpressionParameters
        {
            get
            {
                return parameters;
            }
        }

        public IBoundObject Invoke(IBoundObject Target, IEnumerable<IBoundObject> Arguments)
        {
            if (Body == null)
            {
                return null;
            }
            else
            {
                var args = IsStatic ? BoxHelpers.AutoUnbox(Arguments).ToArray() : BoxHelpers.AutoUnbox(new IBoundObject[] { Target }.Concat(Arguments)).ToArray();

                return BoxHelpers.AutoBox(deleg.Value.DynamicInvoke(args), ReturnType);
            }
        }

        public override void AddParameter(IParameter Parameter)
        {
            base.AddParameter(Parameter);
            parameters.Add(Expression.Parameter(
                ExpressionTypeConverter.Instance.Convert(Parameter.ParameterType), 
                Parameter.Name.ToString()));
        }

        public ICodeGenerator GetBodyGenerator()
        {
            return CodeGenerator;
        }

        public void SetMethodBody(ICodeBlock Body)
        {
            this.Body = CodeGenerator.EmitLambda((IExpressionBlock)Body);
            deleg = new Lazy<Delegate>(() => this.Body.Compile());
        }

        public IMethod Build()
        {
            return this;
        }

        public void Initialize()
        {
            // No need to take action.
        }
    }
}
