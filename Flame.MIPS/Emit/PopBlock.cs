using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class PopBlock : IAssemblerBlock
    {
        public PopBlock(IAssemblerBlock Target)
        {
            this.Target = Target;
        }

        public IAssemblerBlock Target { get; private set; }

        public IType Type
        {
            get { return PrimitiveTypes.Void; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var results = Target.Emit(Context).ToArray();
            var last = results[results.Length - 1];
            last.EmitRelease().Emit(Context);
            return results.Take(results.Length - 1);
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Target.CodeGenerator; }
        }
    }
}
