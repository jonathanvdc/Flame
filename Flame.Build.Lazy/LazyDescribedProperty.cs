using System;
using System.Collections.Generic;
using Flame.Compiler;
using System.Threading;

namespace Flame.Build.Lazy
{
    public class LazyDescribedProperty : LazyDescribedTypeMember, IProperty
    {
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
        
        protected override void CreateBody()
        {
            analyzeBody.Initialize(this);
        }

        public virtual void AddParameter(IParameter Parameter)
        {
            parameters.Add(Parameter);
        }

        public IEnumerable<IParameter> IndexerParameters
        {
            get
            {
                CreateBody();
                return parameters;
            }
        }

        public IEnumerable<IAccessor> Accessors
        {
            get
            {
                CreateBody();
                return accessors;
            }
        }

        public void AddAccessor(IAccessor Accessor)
        {
            this.accessors.Add(Accessor);
        }
    }
}
