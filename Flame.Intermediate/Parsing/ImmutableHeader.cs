using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Parsing
{
    public class ImmutableHeader
    {
        public ImmutableHeader(IBinder ExternalBinder, IReadOnlyList<INodeStructure<IType>> TypeTable,
            IReadOnlyList<INodeStructure<IMethod>> MethodTable, IReadOnlyList<INodeStructure<IField>> FieldTable)
        {
            this.ExternalBinder = ExternalBinder;
            this.TypeTable = TypeTable;
            this.MethodTable = MethodTable;
            this.FieldTable = FieldTable;
        }

        public IBinder ExternalBinder { get; private set; }
        public IReadOnlyList<INodeStructure<IType>> TypeTable { get; private set; }
        public IReadOnlyList<INodeStructure<IMethod>> MethodTable { get; private set; }
        public IReadOnlyList<INodeStructure<IField>> FieldTable { get; private set; }
    }
}
