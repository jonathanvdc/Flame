using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilModuleBinder : BinderBase
    {
        public CecilModuleBinder(CecilModule Module)
        {
            this.Module = Module;
            this.types = new Dictionary<string, IType>();
            foreach (var item in Module.Types)
            {
                AddType(item);
            }
        }

        public CecilModule Module { get; private set; }
        private Dictionary<string, IType> types;

        public override IEnumerable<IType> GetTypes()
        {
            return Module.Types;
        }

        public void AddType(IType Type)
        {
            this.types[Type.FullName] = Type;
        }

        public override IType BindTypeCore(string Name)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return null;
            }

            IType result;
            if (types.TryGetValue(Name, out result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }

        public override IEnvironment Environment
        {
            get { return new CecilEnvironment(Module); }
        }
    }
}
