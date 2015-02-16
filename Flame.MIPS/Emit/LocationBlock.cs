using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class LocationBlock : IAssemblerBlock
    {
        public LocationBlock(ICodeGenerator CodeGenerator, IStorageLocation Location)
        {
            this.CodeGenerator = CodeGenerator;
            this.Location = Location;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IStorageLocation Location { get; private set; }

        public IType Type
        {
            get { return Location.Type; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            return new IStorageLocation[] { Location };
        }
    }
}
