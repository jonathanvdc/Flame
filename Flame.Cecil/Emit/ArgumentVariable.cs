using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ArgumentVariable : UnmanagedVariableBase
    {
        public ArgumentVariable(ICodeGenerator CodeGenerator, int Index)
            : base(CodeGenerator)
        {
            this.Index = Index;
        }

        public int Index { get; private set; }
        public override IType Type
        {
            get
            {
                return ILCodeGenerator.GetExtendedParameterType(CodeGenerator, Index);
            }
        }

        public override void EmitLoad(IEmitContext Context)
        {
            if (Index == 0)
            {
                Context.Emit(OpCodes.Ldarg_0);
            }
            else if (Index == 1)
            {
                Context.Emit(OpCodes.Ldarg_1);
            }
            else if (Index == 2)
            {
                Context.Emit(OpCodes.Ldarg_2);
            }
            else if (Index == 3)
            {
                Context.Emit(OpCodes.Ldarg_3);
            }
            else if (ILCodeGenerator.IsBetween(Index, byte.MinValue, byte.MaxValue))
            {
                Context.Emit(OpCodes.Ldarg_S, (byte)Index);
            }
            else
            {
                Context.Emit(OpCodes.Ldarg, Index);
            }
        }

        public override void EmitAddress(IEmitContext Context)
        {
            if (ILCodeGenerator.IsBetween(Index, byte.MinValue, byte.MaxValue))
            {
                Context.Emit(OpCodes.Ldarga_S, (byte)Index);
            }
            else
            {
                Context.Emit(OpCodes.Ldarga, Index);
            }
        }

        public override void EmitStore(IEmitContext Context, ICecilBlock Value)
        {
            Value.Emit(Context);
            Context.Stack.Pop();
            if (ILCodeGenerator.IsBetween(Index, byte.MinValue, byte.MaxValue))
            {
                Context.Emit(OpCodes.Starg_S, (byte)Index);
            }
            else
            {
                Context.Emit(OpCodes.Starg, Index);
            }
        }
    }
}
