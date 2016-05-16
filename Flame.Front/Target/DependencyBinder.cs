using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class DependencyBinder : IBinder
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

        public IType BindType(QualifiedName Name)
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

        public IEnvironment Environment { get { return Builder.Environment; } }

        public IEnumerable<IType> GetTypes()
        {
            return Binders.SelectMany((item) => item.GetTypes());
        }
    }
}
