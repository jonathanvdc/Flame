using System;
using Flame.Compiler;
using System.Threading;

namespace Flame.Build.Lazy
{
    /// <summary>
    /// A field implementation that constructs itself lazily.
    /// </summary>
    public sealed class LazyDescribedField : LazyDescribedTypeMember, IInitializedField
    {
        /// <summary>
        /// Creates a new lazily constructed field from the given name,
        /// declaring type and deferred construction action.
        /// </summary>
        public LazyDescribedField(
            UnqualifiedName Name, IType DeclaringType,
            Action<LazyDescribedField> AnalyzeBody)
            : base(Name, DeclaringType)
        {
            this.analyzeBody = new DeferredInitializer<LazyDescribedField>(AnalyzeBody);
        }

        private DeferredInitializer<LazyDescribedField> analyzeBody;

        private IType fieldTy;

        /// <summary>
        /// Gets or sets this field's type.
        /// </summary>
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

        private IExpression bodyExpr;

        /// <summary>
        /// Gets or sets this field's (initial) value, as an expression.
        /// </summary>
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

        /// <summary>
        /// Gets this field's (initial) value, as an expression.
        /// </summary>
        public IExpression InitialValue
        {
            get { return Value; }
        }

        /// <summary>
        /// Constructs the initial state of this lazily described member.
        /// This method is called on-demand.
        /// </summary>
        protected override void CreateBody()
        {
            analyzeBody.Initialize(this);
        }
    }
}
