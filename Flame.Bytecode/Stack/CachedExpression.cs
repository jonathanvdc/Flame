using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Bytecode.Stack
{
    public struct CachedExpression
    {
        public IStatement InitializedCacheStatement;
        public IExpression RetrieveExpression;
    }
}
