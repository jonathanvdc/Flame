using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public class StdInitializerList : PrimitiveBase
    {
        private StdInitializerList()
        {
            this.ElementType = new DescribedGenericParameter("T", this);
        }

        private static StdInitializerList inst;
        public static StdInitializerList Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new StdInitializerList();
                }
                return inst;
            }
        }

        public IGenericParameter ElementType { get; private set; }

        public override string Name
        {
            get { return "initializer_list<>"; }
        }

        public override IEnumerable<IAttribute> Attributes
        {
            get
            {
                return new IAttribute[] 
                { 
                    new AccessAttribute(AccessModifier.Public), 
                    PrimitiveAttributes.Instance.ValueTypeAttribute, 
                    new HeaderDependencyAttribute(StandardDependency.InitializerList) 
                };
            }

        }

        public override INamespace DeclaringNamespace
        {
            get { return StdNamespace.Instance; }
        }

        public override IEnumerable<IGenericParameter> GenericParameters
        {
            get { return new IGenericParameter[] { ElementType }; }
        }
    }
}
