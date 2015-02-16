using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.IL.Emit
{
    public abstract class FieldInstruction : ILInstruction
    {
        public FieldInstruction(ICodeGenerator CodeGenerator, IField Field, ICodeBlock Target)
            : base(CodeGenerator)
        {
            this.Field = Field;
            this.Target = (IInstruction)Target;
        }

        public IField Field { get; private set; }
        public IInstruction Target { get; private set; }

        public abstract OpCode OpCode { get; }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            if (Target != null)
            {
                Target.Emit(Context, TypeStack);
            }
            Context.Emit(OpCode, Field);
            UpdateStack(TypeStack);
        }

        public abstract void UpdateStack(Stack<IType> TypeStack);
    }
    public class FieldGetInstruction : FieldInstruction
    {
        public FieldGetInstruction(ICodeGenerator CodeGenerator, IField Field, ICodeBlock Target)
            : base(CodeGenerator, Field, Target)
        {
        }

        public override OpCode OpCode
        {
            get
            {
                if (Field.IsStatic)
                {
                    return OpCodes.LoadStaticField;
                }
                else
                {
                    return OpCodes.LoadField;
                }
            }
        }

        public override void UpdateStack(Stack<IType> TypeStack)
        {
            if (Target != null)
            {
                TypeStack.Pop();
            }
            TypeStack.Push(Field.FieldType);
        }
    }

    public class FieldAddressOfInstruction : FieldInstruction
    {
        public FieldAddressOfInstruction(ICodeGenerator CodeGenerator, IField Field, ICodeBlock Target)
            : base(CodeGenerator, Field, Target)
        {
        }

        public override OpCode OpCode
        {
            get
            {
                if (Field.IsStatic)
                {
                    return OpCodes.LoadStaticFieldAddress;
                }
                else
                {
                    return OpCodes.LoadFieldAddress;
                }
            }
        }

        public override void UpdateStack(Stack<IType> TypeStack)
        {
            if (Target != null)
            {
                TypeStack.Pop();
            }
            TypeStack.Push(Field.FieldType.MakePointerType(PointerKind.ReferencePointer));
        }
    }

    public class FieldSetInstruction : FieldInstruction
    {
        public FieldSetInstruction(ICodeGenerator CodeGenerator, IField Field, ICodeBlock Target, ICodeBlock Value)
            : base(CodeGenerator, Field, Target)
        {
            this.Value = (IInstruction)Value;
        }

        public IInstruction Value { get; private set; }

        public override OpCode OpCode
        {
            get
            {
                if (Field.IsStatic)
                {
                    return OpCodes.StoreStaticField;
                }
                else
                {
                    return OpCodes.StoreField;
                }
            }
        }

        public override void Emit(ICommandEmitContext Context, Stack<IType> TypeStack)
        {
            Target.Emit(Context, TypeStack);
            Value.Emit(Context, TypeStack);
            Context.Emit(OpCode, Field);
            UpdateStack(TypeStack);
        }

        public override void UpdateStack(Stack<IType> TypeStack)
        {
            if (Target != null)
            {
                TypeStack.Pop();
            }
            TypeStack.Pop();
        }
    }
}
