using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class UnaryOpBlock : ICecilBlock
    {
        public UnaryOpBlock(ICodeGenerator CodeGenerator, ICecilBlock Value, Operator Operator)
        {
            this.CodeGenerator = CodeGenerator;
            this.Value = Value;
            this.Operator = Operator;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICecilBlock Value { get; private set; }
        public Operator Operator { get; private set; }

        public static void EmitBooleanNot(IEmitContext Context)
        {
            if (!Context.ApplyOptimization(new BooleanNotOptimization()))
            {
                Context.Emit(OpCodes.Ldc_I4_0);
                Context.Emit(OpCodes.Ceq);
            }
        }

        public void Emit(IEmitContext Context)
        {
            Value.Emit(Context);
            OpCode opCode;
            var type = Context.Stack.Peek();
            if (TryGetOpCode(Operator, type, out opCode))
            {
                bool optimized = false;
                if (opCode == OpCodes.Not && Context.ApplyOptimization(new NotOptimization()))
                {
                    optimized = true;
                }
                if (!optimized)
                {
                    Context.Emit(opCode);
                }
            }
            else if (Operator.Equals(Operator.Not) && type.Equals(PrimitiveTypes.Boolean))
            {
                EmitBooleanNot(Context);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public IType BlockType
        {
            get { return Value.BlockType; }
        }

        #region GetOpCode

        public static bool TryGetOpCode(Operator Op, IType Type, out OpCode Result)
        {
            if (Op.Equals(Operator.Not) && !Type.Equals(PrimitiveTypes.Boolean))
            {
                Result = OpCodes.Not;
                return true;
            }
            else if (Op.Equals(Operator.Subtract))
            {
                Result = OpCodes.Neg;
                return true;
            }
            else
            {
                Result = default(OpCode);
                return false;
            }
        }

        public static bool Supports(Operator Op, IType Type)
        {
            OpCode result;
            return (Op.Equals(Operator.Not) && Type.Equals(PrimitiveTypes.Boolean)) || TryGetOpCode(Op, Type, out result);
        }

        #endregion
    }
}
