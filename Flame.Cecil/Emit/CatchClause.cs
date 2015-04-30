using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class CatchHeader : ICatchHeader
    {
        public CatchHeader(IType ExceptionType, IEmitVariable ExceptionVariable)
        {
            this.ExceptionType = ExceptionType;
            this.ExceptionVariable = ExceptionVariable;
        }

        public IEmitVariable ExceptionVariable { get; private set; }
        public IType ExceptionType { get; private set; }
    }

    public class CatchClause : ICatchClause
    {
        public CatchClause(CatchHeader Header, ICecilBlock Body)
        {
            this.Header = Header;
            this.Body = Body;
        }

        public CatchHeader Header { get; private set; }
        public ICecilBlock Body { get; private set; }

        ICatchHeader ICatchClause.Header
        {
            get { return Header; }
        }
    }
}
