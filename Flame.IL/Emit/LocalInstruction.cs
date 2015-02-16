using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    #region LocalInstruction

    public abstract class LocalInstruction : ILInstruction
    {
        public LocalInstruction(ICodeGenerator CodeGenerator, ILLocalVariable LocalVariable)
            : base(CodeGenerator)
        {
            this.LocalVariable = LocalVariable;
        }

        public ILLocalVariable LocalVariable { get; private set; }

        public abstract OpCode OpCode { get; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            LocalVariable.Bind(Context);

            var opCode = OpCode;
            if (opCode.DataSize == 0)
            {
                Context.Emit(OpCode);
            }
            else
            {
                Context.Emit(OpCode, LocalVariable.EmitLocal);
            }
            UpdateStack(TypeStack);
        }

        protected abstract void UpdateStack(Stack<IType> TypeStack);
    }

    #endregion

    #region LocalGetInstruction

    public class LocalGetInstruction : LocalInstruction
    {
        public LocalGetInstruction(ICodeGenerator CodeGenerator, ILLocalVariable LocalVariable)
            : base(CodeGenerator, LocalVariable)
        {
        }

        public override OpCode OpCode
        {
            get
            {
                /*switch (Index)
                {
                    case 0:
                        return OpCodes.LoadLocal_0;
                    case 1:
                        return OpCodes.LoadLocal_1;
                    case 2:
                        return OpCodes.LoadLocal_2;
                    case 3:
                        return OpCodes.LoadLocal_3;
                    default:
                        break;
                }
                if (Index <= sbyte.MaxValue && Index >= sbyte.MinValue)
                {
                    return OpCodes.LoadLocalShort;
                }
                else
                {*/
                return OpCodes.LoadLocal;
                //}
            }
        }

        protected override void UpdateStack(Stack<IType> TypeStack)
        {
            TypeStack.Push(LocalVariable.Type);
        }
    }

    #endregion

    #region LocalAddressOfInstruction

    public class LocalAddressOfInstruction : LocalInstruction
    {
        public LocalAddressOfInstruction(ICodeGenerator CodeGenerator, ILLocalVariable LocalVariable)
            : base(CodeGenerator, LocalVariable)
        {
        }

        public override OpCode OpCode
        {
            get
            {
                /*if (Index <= sbyte.MaxValue && Index >= sbyte.MinValue)
                {
                    return OpCodes.LoadLocalAddressShort;
                }
                else
                {*/
                    return OpCodes.LoadLocalAddress;
                //}
            }
        }

        protected override void UpdateStack(Stack<IType> TypeStack)
        {
            TypeStack.Push(LocalVariable.Type.MakePointerType(PointerKind.ReferencePointer));
        }
    }

    #endregion

    #region LocalSetInstruction

    public class LocalSetInstruction : LocalInstruction
    {
        public LocalSetInstruction(ICodeGenerator CodeGenerator, ILLocalVariable LocalVariable, ICodeBlock Value)
            : base(CodeGenerator, LocalVariable)
        {
            this.Value = (IInstruction)Value;
        }

        public IInstruction Value { get; private set; }

        public override OpCode OpCode
        {
            get
            {
                /*switch (Index)
                {
                    case 0:
                        return OpCodes.StoreLocal_0;
                    case 1:
                        return OpCodes.StoreLocal_1;
                    case 2:
                        return OpCodes.StoreLocal_2;
                    case 3:
                        return OpCodes.StoreLocal_3;
                    default:
                        break;
                }
                if (Index <= sbyte.MaxValue && Index >= sbyte.MinValue)
                {
                    return OpCodes.StoreLocalShort;
                }
                else
                {*/
                    return OpCodes.StoreLocal;
                //}
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
