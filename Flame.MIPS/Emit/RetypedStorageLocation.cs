using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class RetypedStorageLocation : IStorageLocation
    {
        public RetypedStorageLocation(IStorageLocation Location, IType Type)
        {
            this.Location = Location;
            this.Type = Type;
        }

        public IStorageLocation Location { get; private set; }
        public IType Type { get; private set; }

        public IAssemblerBlock EmitLoad(IRegister Target)
        {
            return Location.EmitLoad(Target);
        }

        public IAssemblerBlock EmitStore(IRegister Target)
        {
            return Location.EmitStore(Target);
        }

        public IAssemblerBlock EmitRelease()
        {
            return Location.EmitRelease();
        }
    }
}
