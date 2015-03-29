using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public class MultiBinder : BinderBase
    {
        public MultiBinder(IEnvironment Environment, IBinder[] Binders)
        {
            this.env = Environment;
            this.Binders = Binders;
        }

        public IBinder[] Binders { get; private set; }

        public override IType BindTypeCore(string Name)
        {
            foreach (var item in Binders)
            {
                var bound = item.BindType(Name);
                if (bound != null)
                {
                    return bound;
                }
            }
            return null;
        }

        private IEnvironment env;
        public override IEnvironment Environment { get { return env; } }

        public override IEnumerable<IType> GetTypes()
        {
            return Binders.SelectMany((item) => item.GetTypes());
        }
    }
}
