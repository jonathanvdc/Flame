using Flame.Compiler;
using Flame.Compiler.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class IfElseBlock : IAssemblerBlock
    {
        public IfElseBlock(ICodeGenerator CodeGenerator, IAssemblerBlock Condition, IAssemblerBlock IfBlock, IAssemblerBlock ElseBlock)
        {
            this.CodeGenerator = CodeGenerator;
            this.Condition = Condition;
            this.IfBlock = IfBlock;
            this.ElseBlock = ElseBlock;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public IAssemblerBlock Condition { get; private set; }
        public IAssemblerBlock IfBlock { get; private set; }
        public IAssemblerBlock ElseBlock { get; private set; }

        public IType Type
        {
            get { return IfBlock.Type; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var brCg = (AssemblerCodeGenerator)CodeGenerator;
            var elseBlock = brCg.CreateLabel();
            var end = brCg.CreateLabel();

            List<IStorageLocation> results = new List<IStorageLocation>();

            ((IAssemblerBlock)elseBlock.EmitBranch(CodeGenerator.EmitNot(Condition))).Emit(Context);
            var ifResults = ((IAssemblerBlock)IfBlock).Emit(Context);
            foreach (var item in ifResults)
            {
                results.Add(item.ReleaseToTemporaryRegister(Context).SpillRegister(Context));
            }
            ((IAssemblerBlock)end.EmitBranch(CodeGenerator.EmitBoolean(true))).Emit(Context);
            ((IAssemblerBlock)elseBlock.EmitMark()).Emit(Context);

            if (results.Count == 1)
            {
                ElseBlock.EmitStoreTo(results[0], Context);
            }
            else
            {
                var elseResults = ((IAssemblerBlock)ElseBlock).Emit(Context).ToArray();
                for (int i = 0; i < elseResults.Length; i++)
                {
                    var temp = elseResults[i].ReleaseToTemporaryRegister(Context);
                    results[i].EmitStore(temp).Emit(Context);
                    temp.EmitRelease().Emit(Context);
                }
            }

            ((IAssemblerBlock)end.EmitMark()).Emit(Context);

            return results;
        }
    }
}
