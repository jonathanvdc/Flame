using Flame.Build;
using Flame.Compiler;
using Flame.ExpressionTrees.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees
{
    public class ExpressionMethod : DescribedMethod, IMethodBuilder
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

        public override IBoundObject Invoke(IBoundObject Target, IEnumerable<IBoundObject> Arguments)
        {
            if (Body == null)
            {
                return null;
            }
            else
            {
                return BoxHelpers.AutoBox(Body.Compile().DynamicInvoke(BoxHelpers.AutoUnbox(new IBoundObject[] { Target }.Concat(Arguments)).ToArray()), ReturnType);
            }
        }

        public override bool Equals(IMethod Other)
        {
            return object.ReferenceEquals(this, Other);
        }

        public override bool Equals(object Other)
        {
            return object.ReferenceEquals(this, Other);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override void AddParameter(IParameter Parameter)
        {
            base.AddParameter(Parameter);
            parameters.Add(Expression.Parameter(ExpressionTypeConverter.Instance.Convert(Parameter.ParameterType), Parameter.Name));
        }

        public ICodeGenerator GetBodyGenerator()
        {
            return CodeGenerator;
        }

        public void SetMethodBody(ICodeBlock Body)
        {
            this.Body = CodeGenerator.EmitLambda((IExpressionBlock)Body);
        }

        public IMethod Build()
        {
            return this;
        }
    }
}
