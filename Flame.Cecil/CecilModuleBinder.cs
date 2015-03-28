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
        }

        public CecilModule Module { get; private set; }

        public override IEnumerable<IType> GetTypes()
        {
            return Module.Types;
        }

        public override IType BindTypeCore(string Name)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return null;
            }
            /*string typeName;
            if (Name.EndsWith(">"))
            {
                int start = Name.IndexOf('<');
                var split = Name.Substring(start + 1, Name.Length - start - 1).Split(',');

                typeName = Name.Substring(0, start) + "`" + split.Length.ToString();
            }
            else
            {
                typeName = Name;
            }*/
            foreach (var item in Module.Types)
            {
                if (item.FullName == Name)
                {
                    return item;
                }
            }
            return null;
        }

        public override IEnvironment Environment
        {
            get { return new CecilEnvironment(Module); }
        }
    }
}
