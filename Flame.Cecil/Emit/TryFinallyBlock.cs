using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class TryFinallyBlock : ICecilBlock
    {
        public TryFinallyBlock(ICecilBlock TryBody, ICecilBlock FinallyBody)
        {
            this.TryBody = TryBody;
            this.FinallyBody = FinallyBody;
        }

        public ICecilBlock TryBody { get; private set; }
        public ICecilBlock FinallyBody { get; private set; }

        public void Emit(IEmitContext Context)
        {
            var blockEndLabel = Context.CreateLabel();

            var tryStartLabel = Context.CreateLabel();
            Context.MarkLabel(tryStartLabel);

            TryBody.Emit(Context);

            Context.Emit(Mono.Cecil.Cil.OpCodes.Leave, blockEndLabel);

            var tryEndLabel = Context.CreateLabel();
            Context.MarkLabel(tryEndLabel);

            var finallyStartLabel = Context.CreateLabel();
            Context.MarkLabel(finallyStartLabel);

            int instrCount = Context.Processor.Body.Instructions.Count;

            FinallyBody.Emit(Context);

            if (Context.Processor.Body.Instructions.Count - instrCount > 0) // Emit a finally block only if the finally body is nonempty
            {
                Context.Emit(Mono.Cecil.Cil.OpCodes.Endfinally);

                var finallyEndLabel = Context.CreateLabel();
                Context.MarkLabel(finallyEndLabel);

                Context.CreateFinallyHandler(tryStartLabel, tryEndLabel, finallyStartLabel, finallyEndLabel);
            }
            else
            {
                Context.PopInstructions(1); // Get rid of the 'leave' instruction
            }

            Context.MarkLabel(blockEndLabel);
        }

        public IStackBehavior StackBehavior
        {
            get { return TryBody.StackBehavior; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return TryBody.CodeGenerator; }
        }
    }
}
