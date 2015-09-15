using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Parsing
{
    // IR file contents:
    // - Type table
    // - Method table
    // - Field table
    // - Assembly

    public class IRParser
    {
        public IBinder ExternalBinder { get; private set; }
    }
}
