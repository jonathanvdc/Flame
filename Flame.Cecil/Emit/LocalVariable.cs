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
    public class LocalVariable : UnmanagedVariableBase
    {
        public LocalVariable(ILCodeGenerator CodeGenerator, IType Type)
            : this(CodeGenerator, Type, null)
        {
        }
        public LocalVariable(ILCodeGenerator CodeGenerator, IType Type, UnqualifiedName Name)
            : base(CodeGenerator)
        {
            this.Member = new DescribedVariableMember(Name, Type);
        }
        public LocalVariable(ILCodeGenerator CodeGenerator, IVariableMember Member)
            : base(CodeGenerator)
        {
            this.Member = Member;
        }

        public IVariableMember Member { get; private set; }
        public UnqualifiedName Name { get { return Member.Name; } }
        public override IType Type { get { return Member.VariableType; } }

        private IEmitLocal emitLocal;
        public IEmitLocal GetEmitLocal(IEmitContext Context)
        {
            if (emitLocal == null)
            {
                emitLocal = Context.DeclareLocal(Type);
                if (!string.IsNullOrWhiteSpace(Name.ToString()))
                {
                    emitLocal.Name = Name.ToString();
                }
            }
            return emitLocal;
        }

        public override void EmitAddress(IEmitContext Context)
        {
            var emitVariable = GetEmitLocal(Context);
            if (ILCodeGenerator.IsBetween(emitVariable.Index, byte.MinValue, byte.MaxValue))
            {
                Context.Emit(OpCodes.Ldloca_S, emitVariable);
            }
            else
            {
                Context.Emit(OpCodes.Ldloca, emitVariable);
            }
        }

        public override void EmitLoad(IEmitContext Context)
        {
            var emitVariable = GetEmitLocal(Context);
            int index = emitVariable.Index;
            if (index == 0)
            {
                Context.Emit(OpCodes.Ldloc_0);
            }
            else if (index == 1)
            {
                Context.Emit(OpCodes.Ldloc_1);
            }
            else if (index == 2)
            {
                Context.Emit(OpCodes.Ldloc_2);
            }
            else if (index == 3)
            {
                Context.Emit(OpCodes.Ldloc_3);
            }
            else if (ILCodeGenerator.IsBetween(index, byte.MinValue, byte.MaxValue))
            {
                Context.Emit(OpCodes.Ldloc_S, emitVariable);
            }
            else
            {
                Context.Emit(OpCodes.Ldloc, emitVariable);
            }
        }

        public override void EmitStore(IEmitContext Context, ICecilBlock Value)
        {
            Value.Emit(Context);
            Context.Stack.Pop();
            var emitVariable = GetEmitLocal(Context);
            int index = emitVariable.Index;
            if (index == 0)
            {
                Context.Emit(OpCodes.Stloc_0);
            }
            else if (index == 1)
            {
                Context.Emit(OpCodes.Stloc_1);
            }
            else if (index == 2)
            {
                Context.Emit(OpCodes.Stloc_2);
            }
            else if (index == 3)
            {
                Context.Emit(OpCodes.Stloc_3);
            }
            else if (ILCodeGenerator.IsBetween(index, byte.MinValue, byte.MaxValue))
            {
                Context.Emit(OpCodes.Stloc_S, emitVariable);
            }
            else
            {
                Context.Emit(OpCodes.Stloc, emitVariable);
            }
        }
    }
}
