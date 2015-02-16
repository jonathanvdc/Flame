using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Analysis
{
    public class VariableProperties : IVariableProperties
    {
        public VariableProperties(bool IsLocal)
        {
            this.IsLocal = IsLocal;
        }

        public bool IsLocal { get; private set; }
    }
}
