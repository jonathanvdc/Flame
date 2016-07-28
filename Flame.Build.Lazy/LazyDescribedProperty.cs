using System;
using System.Collections.Generic;
using Flame.Compiler;
using System.Threading;

namespace Flame.Build.Lazy
{
    /// <summary>
    /// A property implementation that constructs itself lazily.
    /// </summary>
    public class LazyDescribedProperty : LazyDescribedTypeMember, IProperty
    {
        /// <summary>
        /// Creates a new lazily described property from the given name,
        /// declaring type, and a deferred construction action.
        /// </summary>
        public LazyDescribedProperty(
            UnqualifiedName Name, IType DeclaringType,
            Action<LazyDescribedProperty> AnalyzeBody)
            : base(Name, DeclaringType)
        {
            this.parameters = new List<IParameter>();
            this.accessors = new List<IAccessor>();
            this.analyzeBody = new DeferredInitializer<LazyDescribedProperty>(AnalyzeBody);
        }

        private DeferredInitializer<LazyDescribedProperty> analyzeBody;

        private IType retType;
        private List<IParameter> parameters;
        private List<IAccessor> accessors;

        /// <summary>
        /// Gets or sets this property's type.
        /// </summary>
        public IType PropertyType
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

        /// <summary>
        /// Constructs the initial state of this lazily described member.
        /// This method is called on-demand.
        /// </summary>
        protected override void CreateBody()
        {
            analyzeBody.Initialize(this);
        }

        /// <summary>
        /// Adds a parameter to this property's indexer parameter list.
        /// </summary>
        public virtual void AddParameter(IParameter Parameter)
        {
            CreateBody();
            parameters.Add(Parameter);
        }

        /// <summary>
        /// Gets this property's indexer parameter list.
        /// </summary>
        public IEnumerable<IParameter> IndexerParameters
        {
            get
            {
                CreateBody();
                return parameters;
            }
        }

        /// <summary>
        /// Gets this property's accessor list.
        /// </summary>
        public IEnumerable<IAccessor> Accessors
        {
            get
            {
                CreateBody();
                return accessors;
            }
        }

        /// <summary>
        /// Adds an accessor to this property's accessor list.
        /// </summary>
        public void AddAccessor(IAccessor Accessor)
        {
            CreateBody();
            this.accessors.Add(Accessor);
        }
    }
}
