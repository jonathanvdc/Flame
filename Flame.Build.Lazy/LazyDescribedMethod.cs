using System;
using Flame.Build;
using System.Collections.Generic;
using Flame.Compiler;
using System.Threading;

namespace Flame.Build.Lazy
{
    /// <summary>
    /// A method implementation that constructs itself lazily.
    /// </summary>
    public class LazyDescribedMethod : LazyDescribedTypeMember, IBodyMethod
    {
        /// <summary>
        /// Creates a new lazily described method from the given name,
        /// declaring type, and a deferred construction action.
        /// </summary>
        public LazyDescribedMethod(
            UnqualifiedName Name, IType DeclaringType,
            Action<LazyDescribedMethod> AnalyzeHeader,
            Action<LazyDescribedMethod> AnalyzeBaseMethods,
            Action<LazyDescribedMethod> AnalyzeBody)
            : base(Name, DeclaringType)
        {
            this.baseMethods = new List<IMethod>();
            this.parameters = new List<IParameter>();
            this.baseMethods = new List<IMethod>();
            this.genericParams = new List<IGenericParameter>();
            this.analyzeHeader = new DeferredInitializer<LazyDescribedMethod>(AnalyzeHeader);
            this.analyzeBaseMethods = new DeferredInitializer<LazyDescribedMethod>(AnalyzeBaseMethods);
            this.analyzeBody = new DeferredInitializer<LazyDescribedMethod>(AnalyzeBody);
        }

        /// <summary>
        /// Creates a new lazily described method from the given name,
        /// declaring type, and a deferred construction action.
        /// </summary>
        public LazyDescribedMethod(
            UnqualifiedName Name, IType DeclaringType,
            Action<LazyDescribedMethod> AnalyzeBody)
            : base(Name, DeclaringType)
        {
            this.baseMethods = new List<IMethod>();
            this.parameters = new List<IParameter>();
            this.baseMethods = new List<IMethod>();
            this.genericParams = new List<IGenericParameter>();
            this.analyzeHeader = new DeferredInitializer<LazyDescribedMethod>(AnalyzeBody);
            this.analyzeBaseMethods = new DeferredInitializer<LazyDescribedMethod>(x => analyzeHeader.Initialize(x));
            this.analyzeBody = new DeferredInitializer<LazyDescribedMethod>(x => analyzeHeader.Initialize(x));
        }

        private DeferredInitializer<LazyDescribedMethod> analyzeHeader;
        private DeferredInitializer<LazyDescribedMethod> analyzeBaseMethods;
        private DeferredInitializer<LazyDescribedMethod> analyzeBody;

        private IType retType;

        /// <summary>
        /// Gets or sets this method's return type.
        /// </summary>
        public IType ReturnType
        {
            get
            {
                CreateBody();
                return retType;
            }
            set
            {
                CreateBody();
                retType = value;
            }
        }

        private bool isCtorVal;

        /// <summary>
        /// Gets or sets a boolean flag that tells if this method is a constructor.
        /// </summary>
        public bool IsConstructor
        {
            get
            {
                CreateBody();
                return isCtorVal;
            }
            set
            {
                CreateBody();
                isCtorVal = value;
            }
        }

        private IStatement bodyStmt;

        /// <summary>
        /// Gets or sets this method's body statement.
        /// </summary>
        public IStatement Body
        {
            get
            {
                analyzeBody.Initialize(this);
                return bodyStmt;
            }
            set
            {
                analyzeBody.Initialize(this);
                bodyStmt = value;
            }
        }

        /// <summary>
        /// Gets the method's body statement.
        /// </summary>
        public IStatement GetMethodBody()
        {
            return Body;
        }

        /// <summary>
        /// Constructs the initial state of this lazily described member.
        /// This method is called on-demand.
        /// </summary>
        protected override void CreateBody()
        {
            analyzeHeader.Initialize(this);
        }

        private List<IParameter> parameters;

        /// <summary>
        /// Adds the given parameter to this method's parameter list.
        /// </summary>
        public virtual void AddParameter(IParameter Parameter)
        {
            CreateBody();
            parameters.Add(Parameter);
        }

        /// <summary>
        /// Gets this method's parameter list.
        /// </summary>
        public IEnumerable<IParameter> Parameters
        {
            get
            {
                CreateBody();
                return parameters;
            }
        }

        private List<IMethod> baseMethods;

        /// <summary>
        /// Makes the given method a base method of this method.
        /// </summary>
        public virtual void AddBaseMethod(IMethod Method)
        {
            analyzeBaseMethods.Initialize(this);
            baseMethods.Add(Method);
        }

        /// <summary>
        /// Gets the list of all base methods for this method.
        /// </summary>
        public IEnumerable<IMethod> BaseMethods
        {
            get
            {
                analyzeBaseMethods.Initialize(this);
                return baseMethods.ToArray();
            }
        }

        private List<IGenericParameter> genericParams;

        /// <summary>
        /// Adds the given generic parameter to this method's generic parameter
        /// list.
        /// </summary>
        public virtual void AddGenericParameter(IGenericParameter Parameter)
        {
            CreateBody();
            genericParams.Add(Parameter);
        }

        /// <summary>
        /// Gets this method's list of generic parameters.
        /// </summary>
        public IEnumerable<IGenericParameter> GenericParameters
        {
            get
            {
                CreateBody();
                return genericParams;
            }
        }
    }
}
