using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class ReturnBlock : IAssemblerBlock
    {
        public ReturnBlock(IAssemblerBlock FirstReturnValue, params IAssemblerBlock[] OtherReturnValues)
        {
            this.CodeGenerator = FirstReturnValue.CodeGenerator;
            this.ReturnValues = new IAssemblerBlock[] { FirstReturnValue }.Concat(OtherReturnValues);
        }
        public ReturnBlock(ICodeGenerator CodeGenerator, params IAssemblerBlock[] ReturnValues)
        {
            this.CodeGenerator = CodeGenerator;
            this.ReturnValues = ReturnValues;
        }
        public ReturnBlock(ICodeGenerator CodeGenerator, IEnumerable<IAssemblerBlock> ReturnValues)
        {
            this.CodeGenerator = CodeGenerator;
            this.ReturnValues = ReturnValues;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IEnumerable<IAssemblerBlock> ReturnValues { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            List<IStorageLocation> locations = new List<IStorageLocation>();
            foreach (var item in ReturnValues)
            {
                locations.AddRange(item.Emit(Context));
            }
            Context.EmitReturn(locations);
            return new IStorageLocation[0];
        }
    }
}
