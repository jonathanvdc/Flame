using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class DependencyBinder : BinderBase
    {
        public DependencyBinder(DependencyBuilder Builder)
        {
            this.Builder = Builder;
            this.binders = new ConcurrentDictionary<IAssembly, IBinder>();
        }

        private ConcurrentDictionary<IAssembly, IBinder> binders;

        public DependencyBuilder Builder { get; private set; }

        public IEnumerable<IBinder> Binders
        {
            get
            {
                return Builder.RegisteredAssemblies.Select(item =>
                    binders.GetOrAdd(item, asm => asm.CreateBinder()));
            }
        }

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

        public override IEnvironment Environment { get { return Builder.Environment; } }

        public override IEnumerable<IType> GetTypes()
        {
            return Binders.SelectMany((item) => item.GetTypes());
        }
    }
}
