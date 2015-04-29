using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class TryBlock : ICecilBlock
    {
        public TryBlock(ICecilBlock TryBody, ICecilBlock FinallyBody, IEnumerable<CatchClause> CatchClauses)
        {
            this.TryBody = TryBody;
            this.FinallyBody = FinallyBody;
            this.CatchClauses = CatchClauses;
        }

        public ICecilBlock TryBody { get; private set; }
        public ICecilBlock FinallyBody { get; private set; }
        public IEnumerable<CatchClause> CatchClauses { get; private set; }

        public void Emit(IEmitContext Context)
        {
            var blockEndLabel = Context.CreateLabel();

            var tryStartLabel = Context.CreateLabel();
            Context.MarkLabel(tryStartLabel);

            TryBody.Emit(Context);

            Context.Emit(Mono.Cecil.Cil.OpCodes.Leave, blockEndLabel);

            var tryEndLabel = Context.CreateLabel();
            Context.MarkLabel(tryEndLabel);

            foreach (var item in CatchClauses)
            {
                var catchStartLabel = Context.CreateLabel();
                Context.MarkLabel(catchStartLabel);

                Context.Stack.Push(item.Header.ExceptionType); // Push exception type on stack
                ((ICecilBlock)item.Header.ExceptionVariable.EmitSet(CodeGenerator.EmitVoid())).Emit(Context); // Exception reference is pushed on the stack

                item.Body.Emit(Context);

                Context.Emit(Mono.Cecil.Cil.OpCodes.Leave, blockEndLabel);

                var catchEndLabel = Context.CreateLabel();
                Context.MarkLabel(catchEndLabel);

                Context.CreateCatchHandler(tryStartLabel, tryEndLabel, catchStartLabel, catchEndLabel, item.Header.ExceptionType);
            }

            var finallyStartLabel = Context.CreateLabel();
            Context.MarkLabel(finallyStartLabel);

            int instrCount = Context.Processor.Body.Instructions.Count;

            FinallyBody.Emit(Context);

            if (Context.Processor.Body.Instructions.Count - instrCount > 0)
            {
                Context.Emit(Mono.Cecil.Cil.OpCodes.Endfinally);

                var finallyEndLabel = Context.CreateLabel();
                Context.MarkLabel(finallyEndLabel);

                Context.CreateFinallyHandler(tryStartLabel, tryEndLabel, finallyStartLabel, finallyEndLabel);
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
