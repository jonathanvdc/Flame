using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    #region ArgumentInstruction

    public abstract class ArgumentInstruction : ILInstruction
    {
        public ArgumentInstruction(ICodeGenerator CodeGenerator, int Index)
            : base(CodeGenerator)
        {
            this.Index = Index;
        }

        public int Index { get; private set; }
        public IType Type
        {
            get
            {
                return CodeGenerator.Method.GetParameters()[Index].ParameterType;
            }
        }

        public abstract OpCode OpCode { get; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            var opCode = OpCode;
            if (opCode.DataSize == 0)
            {
                Context.Emit(OpCode);
            }
            else if (opCode.DataSize == 1)
            {
                Context.Emit(OpCode, (sbyte)Index);
            }
            else
            {
                Context.Emit(OpCode, (short)Index);
            }
            UpdateStack(TypeStack);
        }

        protected abstract void UpdateStack(Stack<IType> TypeStack);
    }

    #endregion

    #region ArgumentGetInstruction

    public class ArgumentGetInstruction : ArgumentInstruction
    {
        public ArgumentGetInstruction(ICodeGenerator CodeGenerator, int Index)
            : base(CodeGenerator, Index)
        {
        }

        public override OpCode OpCode
        {
            get
            {
                switch (Index)
                {
                    case 0:
                        return OpCodes.LoadArgument_0;
                    case 1:
                        return OpCodes.LoadArgument_1;
                    case 2:
                        return OpCodes.LoadArgument_2;
                    case 3:
                        return OpCodes.LoadArgument_3;
                    default:
                        break;
                }
                if (Index <= sbyte.MaxValue && Index >= sbyte.MinValue)
                {
                    return OpCodes.LoadArgumentShort;
                }
                else
                {
                    return OpCodes.LoadArgument;
                }
            }
        }

        protected override void UpdateStack(Stack<IType> TypeStack)
        {
            TypeStack.Push(Type);
        }
    }

    #endregion

    #region ArgumentAddressOfInstruction

    public class ArgumentAddressOfInstruction : ArgumentInstruction
    {
        public ArgumentAddressOfInstruction(ICodeGenerator CodeGenerator, int Index)
            : base(CodeGenerator, Index)
        {
        }

        public override OpCode OpCode
        {
            get
            {
                if (Index <= sbyte.MaxValue && Index >= sbyte.MinValue)
                {
                    return OpCodes.LoadArgumentAddressShort;
                }
                else
                {
                    return OpCodes.LoadArgumentAddress;
                }
            }
        }

        protected override void UpdateStack(Stack<IType> TypeStack)
        {
            TypeStack.Push(Type.MakePointerType(PointerKind.ReferencePointer));
        }
    }

    #endregion

    #region ArgumentSetInstruction

    public class ArgumentSetInstruction : ArgumentInstruction
    {
        public ArgumentSetInstruction(ICodeGenerator CodeGenerator, int Index, ICodeBlock Value)
            : base(CodeGenerator, Index)
        {
            this.Value = (IInstruction)Value;
        }

        public IInstruction Value { get; private set; }

        public override OpCode OpCode
        {
            get
            {
                if (Index <= sbyte.MaxValue && Index >= sbyte.MinValue)
                {
                    return OpCodes.StoreArgumentShort;
                }
                else
                {
                    return OpCodes.StoreArgument;
                }
            }
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Value.Emit(Context, TypeStack);
            base.Emit(Context, TypeStack);
        }

        protected override void UpdateStack(Stack<IType> TypeStack)
        {
            TypeStack.Pop();
            TypeStack.Pop();
        }
    }

    #endregion
}
