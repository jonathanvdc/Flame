using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class NotBlock : IAssemblerBlock
    {
        public NotBlock(IAssemblerBlock Value)
        {
            this.Value = Value;
        }

        public IAssemblerBlock Value { get; private set; }

        public IType Type
        {
            get { return Value.Type; }
        }

        public ICodeGenerator CodeGenerator
        {
            get { return Value.CodeGenerator; }
        }

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            if (Value is BinaryOpBlock)
            {
                var binBlock = (BinaryOpBlock)Value;
                Operator neg;
                if (TryGetNegatedOperator(binBlock.Operator, out neg))
                {
                    return new BinaryOpBlock(CodeGenerator, binBlock.Left, neg, binBlock.Right).Emit(Context);
                }
            }
            var rVal = Value.EmitToRegister(Context).ReleaseToTemporaryRegister(Context);
            Context.Emit(new Instruction(OpCodes.XorImmediate, new IInstructionArgument[] { Context.ToArgument(rVal), Context.ToArgument(rVal), Context.ToArgument(1) }, "'xors' the value with '1', which amounts to a boolean not"));
            return new IStorageLocation[] { rVal };
        }

        public static bool TryGetNegatedOperator(Operator Op, out Operator Result)
        {
            if (Op.Equals(Operator.CheckEquality))
            {
                Result = Operator.CheckInequality;
            }
            else if (Op.Equals(Operator.CheckGreaterThan))
            {
                Result = Operator.CheckLessThanOrEqual;
            }
            else if (Op.Equals(Operator.CheckLessThan))
            {
                Result = Operator.CheckGreaterThanOrEqual;
            }
            else if (Op.Equals(Operator.CheckLessThanOrEqual))
            {
                Result = Operator.CheckGreaterThan;
            }
            else if (Op.Equals(Operator.CheckGreaterThanOrEqual))
            {
                Result = Operator.CheckLessThan;
            }
            else
            {
                Result = default(Operator);
                return false;
            }
            return true;
        }
    }
}
