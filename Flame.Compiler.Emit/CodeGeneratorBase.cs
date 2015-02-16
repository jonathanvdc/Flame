using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Compiler.Emit
{
    public abstract class CodeGeneratorBase : ICodeGenerator
    {
        public abstract IBlockGenerator CreateBlock();

        public abstract IIfElseBlockGenerator CreateIfElseBlock(ICodeBlock Condition);

        public abstract IFlowBlockGenerator CreateWhileBlock(ICodeBlock Condition);

        public abstract ICodeBlock EmitAdd(ICodeBlock A, ICodeBlock B);

        public abstract ICodeBlock EmitAnd(ICodeBlock A, ICodeBlock B);

        public abstract ICodeBlock EmitBit16(ushort Value);
        public abstract ICodeBlock EmitBit32(uint Value);
        public abstract ICodeBlock EmitBit64(ulong Value);
        public abstract ICodeBlock EmitBit8(byte Value);
        public abstract ICodeBlock EmitBoolean(bool Value);
        public abstract ICodeBlock EmitChar(char Value);

        public abstract ICodeBlock EmitConversion(ICodeBlock Value, IType Type);

        public abstract ICodeBlock EmitDefaultValue(IType Type);

        public abstract ICodeBlock EmitDivide(ICodeBlock A, ICodeBlock B);
        public abstract ICodeBlock EmitEquals(ICodeBlock A, ICodeBlock B);
        public abstract ICodeBlock EmitFloat32(float Value);
        public abstract ICodeBlock EmitFloat64(double Value);
        public abstract ICodeBlock EmitGetField(IField Field, ICodeBlock Target);
        public abstract ICodeBlock EmitGreaterThan(ICodeBlock A, ICodeBlock B);
        public abstract ICodeBlock EmitInt16(short Value);
        public abstract ICodeBlock EmitInt32(int Value);
        public abstract ICodeBlock EmitInt64(long Value);
        public abstract ICodeBlock EmitInt8(sbyte Value);

        public abstract ICodeBlock EmitInvocation(IMethod Method, ICodeBlock Caller, IEnumerable<ICodeBlock> Arguments);
        public abstract ICodeBlock EmitIsOfType(IType Type, ICodeBlock Value);

        public abstract ICodeBlock EmitLeftShift(ICodeBlock A, ICodeBlock B);
        public abstract ICodeBlock EmitLessThan(ICodeBlock A, ICodeBlock B);
        public abstract ICodeBlock EmitMultiply(ICodeBlock A, ICodeBlock B);

        public abstract ICodeBlock EmitNewArray(IType ElementType, IEnumerable<ICodeBlock> Dimensions);
        public abstract ICodeBlock EmitNewVector(IType ElementType, int[] Dimensions);

        public abstract ICodeBlock EmitNot(ICodeBlock Value);
        public abstract ICodeBlock EmitNull();

        public abstract ICodeBlock EmitOr(ICodeBlock A, ICodeBlock B);
        public abstract ICodeBlock EmitRightShift(ICodeBlock A, ICodeBlock B);

        public abstract ICodeBlock EmitString(string Value);

        public abstract ICodeBlock EmitSubtract(ICodeBlock A, ICodeBlock B);

        public abstract ICodeBlock EmitUInt16(ushort Value);
        public abstract ICodeBlock EmitUInt32(uint Value);
        public abstract ICodeBlock EmitUInt64(ulong Value);
        public abstract ICodeBlock EmitUInt8(byte Value);

        public abstract ICodeBlock EmitXor(ICodeBlock A, ICodeBlock B);

        public abstract IVariable GetElement(ICodeBlock Value, IEnumerable<ICodeBlock> Index);

        public abstract IVariable DeclareVariable(IType Type);

        public abstract IVariable GetArgument(int Index);

        public abstract IVariable GetThis();

        public abstract IMethod Method
        {
            get;
        }
    }
}
