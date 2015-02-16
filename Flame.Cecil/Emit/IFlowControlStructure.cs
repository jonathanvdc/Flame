﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public interface IFlowControlStructure
    {
        ICecilBlock CreateBreak();
        ICecilBlock CreateContinue();
    }
}
