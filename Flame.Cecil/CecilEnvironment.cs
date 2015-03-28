using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilEnvironment : IEnvironment
    {
        public CecilEnvironment(CecilModule Module)
        {
            this.Module = Module;
        }

        public CecilModule Module { get; private set; }

        public string Name
        {
            get { return "CLR/Cecil"; }
        }

        public IType RootType
        {
            get { return Module.ConvertStrict(Module.Module.TypeSystem.Object); }
        }

        public IType EnumerableType
        {
            get { return Module.ConvertStrict(typeof(IEnumerable<>)); }
        }

        public IType EnumeratorType
        {
            get { return Module.ConvertStrict(typeof(IEnumerator<>)); }
        }
    }
}
