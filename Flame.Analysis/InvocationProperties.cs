using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class InvocationProperties : IExpressionProperties
    {
        public InvocationProperties(InvocationBlock Block)
        {
            this.Block = Block;
        }

        public InvocationBlock Block { get; private set; }

        private IMethod method;
        public IMethod Method
        {
            get
            {
                if (method == null)
                {
                    if (Block.Target is MethodDelegateBlock)
                    {
                        method = ((MethodDelegateBlock)Block.Target).Method;
                    }
                    else
                    {
                        method = MethodType.GetMethod(Block.Target.ExpressionProperties.Type);
                    }
                }
                return method;
            }
        }

        public IType Type
        {
            get { return Method.ReturnType; }
        }

        public bool Inline
        {
            get { return false; }
        }

        public bool IsConstant
        {
            get { return Method.get_IsConstant(); }
        }

        public bool IsFieldAccessor
        {
            get
            {
                return method.HasAttribute(FieldAccessorAttribute.FieldAccessorAttributeType);
            }
        }

        public IAnalyzedVariable AccessedField
        {
            get
            {
                var attr = (FieldAccessorAttribute)Method.GetAttribute(FieldAccessorAttribute.FieldAccessorAttributeType);
                var singleCaller = Block.Target.GetReturnVariable();
                if (singleCaller != null)
                {
                    var field = Method.DeclaringType.GetField(attr.FieldName);
                    return (IAnalyzedVariable)Block.CodeGenerator.GetField(field, Block.Target);
                }
                else
                {
                    return null;
                }
            }
        }

        public bool IsVolatile
        {
            get
            {
                return !IsConstant && !IsFieldAccessor;
            }
        }
    }
}
