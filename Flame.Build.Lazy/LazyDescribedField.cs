using System;
using Flame.Compiler;
using System.Threading;

namespace Flame.Build.Lazy
{
    public class LazyDescribedField : LazyDescribedTypeMember, IInitializedField
    {
        public LazyDescribedField(UnqualifiedName Name, IType DeclaringType, Action<LazyDescribedField> AnalyzeBody)
            : base(Name, DeclaringType)
        {
            this.analyzeBody = new DeferredInitializer<LazyDescribedField>(AnalyzeBody);
        }

        private DeferredInitializer<LazyDescribedField> analyzeBody;

        private IType fieldTy;

        public IType FieldType
        { 
            get
            {
                CreateBody();
                return fieldTy;
            }
            set
            { 
                CreateBody();
                fieldTy = value;
            }
        }

        private bool isStaticVal;
        public override bool IsStatic
        {
            get
            {
                CreateBody();
                return isStaticVal;
            }
            set
            {
                CreateBody();
                isStaticVal = value;
            }
        }

        private IExpression bodyExpr;

        public IExpression Value
        { 
            get
            {
                CreateBody();
                return bodyExpr;
            }
            set
            { 
                CreateBody();
                bodyExpr = value; 
            }
        }

        public IExpression GetValue()
        {
            return Value;
        }

        protected override void CreateBody()
        {
            analyzeBody.Initialize(this);
        }
    }
}

