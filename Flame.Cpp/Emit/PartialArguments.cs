using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class PartialArguments
    {
        public PartialArguments(params ICppBlock[] Arguments)
        {
            this.Arguments = Arguments;
        }

        public ICppBlock[] Arguments { get; private set; }

        public ICppBlock Get(int Index)
        {
            return Arguments[Index];
        }
    }
}
