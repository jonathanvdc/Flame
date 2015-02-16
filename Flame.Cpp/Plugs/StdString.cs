using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public class StdString : PrimitiveBase
    {
        private StdString()
        {

        }

        public override string Name
        {
            get { return "string"; }
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[] { new AccessAttribute(AccessModifier.Public) };
        }

        public override INamespace DeclaringNamespace
        {
            get { return StdNamespace.Instance; }
        }

        public override IMethod[] GetConstructors()
        {
            throw new NotImplementedException();
        }

        public override IField[] GetFields()
        {
            return new IField[0];
        }

        public override IMethod[] GetMethods()
        {
            throw new NotImplementedException();
        }

        public override IProperty[] GetProperties()
        {
            throw new NotImplementedException();
        }

        private static StdString inst;
        public static StdString Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new StdString();
                }
                return inst;
            }
        }
    }
}
