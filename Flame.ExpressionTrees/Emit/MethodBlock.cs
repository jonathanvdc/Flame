using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Flame.ExpressionTrees.Emit
{
    public class MethodBlock : IExpressionBlock
    {
        public MethodBlock(ExpressionCodeGenerator CodeGenerator, IExpressionBlock Target, IMethod Member, bool IsVirtual)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
            this.Member = Member;
            this.IsVirtual = IsVirtual;
        }

        public ExpressionCodeGenerator CodeGenerator { get; private set; }
        public IExpressionBlock Target { get; private set; }
        public IMethod Member { get; private set; }
        public bool IsVirtual { get; private set; }

        public IType Type
        {
            get { return MethodType.Create(Member); }
        }

        ICodeGenerator ICodeBlock.CodeGenerator
        {
            get { return CodeGenerator; }
        }

        public Expression CreateIndirectCall(FlowStructure Flow)
        {
            Expression<Func<IBoundObject, Func<IBoundObject[], IBoundObject>>> quote =
                target => args => ((IInvocableMethod)Member.GetImplementation(target.Type)).Invoke(target, args);

            return ExpressionCodeGenerator.AutoUnbox(Expression.Invoke(quote, Target.CreateExpression(Flow)), Target.Type);
        }

        public Expression CreateExpression(FlowStructure Flow)
        {
            if (IsVirtual)
            {
                return CreateIndirectCall(Flow);
            }

            if (Member is ExpressionMethod)
            {
                var body = ((ExpressionMethod)Member).Body;

                if (Target == null || Target.Type.Equals(PrimitiveTypes.Null))
                {
                    return body;
                }
                else
                {
                    var paramTypes = Member.GetParameters().Select(item => ExpressionTypeConverter.Instance.Convert(item.ParameterType)).ToArray();
                    var retType = ExpressionTypeConverter.Instance.Convert(Member.ReturnType);

                    var newParameters = body.Parameters.Skip(1).Select(item => Expression.Parameter(item.Type, item.Name)).ToArray();
                    var newArgs = new Expression[] { Target.CreateExpression(Flow) }.Concat(newParameters);

                    return Expression.Lambda(Expression.Invoke(body, newParameters), newParameters);
                }
            }
            else
            {
                var exprType = ExpressionTypeConverter.Instance.Convert(Member.DeclaringType);

                if (exprType == typeof(IBoundObject))
                {
                    return CreateIndirectCall(Flow);
                }
                else
                {
                    var targetMethod = exprType.GetMethod(Member.Name, Member.GetParameters().Select(item => ExpressionTypeConverter.Instance.Convert(item.ParameterType)).ToArray());
                    var targetParamTypes = targetMethod.GetParameters().Select(item => item.ParameterType);

                    var delegateTarget = Expression.Convert(Target.CreateExpression(Flow), typeof(object));

                    return Expression.New(
                        Expression.GetDelegateType(targetParamTypes.Concat(new Type[] { targetMethod.ReturnType }).ToArray()).GetConstructor(new Type[] { typeof(object), typeof(IntPtr) }),
                        new Expression[]
                        {
                            delegateTarget,
                            Expression.Constant(targetMethod.MethodHandle.Value)
                        });
                }
            }
        }
    }
}
