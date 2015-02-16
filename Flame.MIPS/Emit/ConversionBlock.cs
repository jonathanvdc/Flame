using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class ConversionBlock : IAssemblerBlock
    {
        public ConversionBlock(IAssemblerBlock Value, IType Type)
        {
            this.Value = Value;
            this.Type = Type;
        }

        public IAssemblerBlock Value { get; private set; }
        public IType Type { get; private set; }

        public ICodeGenerator CodeGenerator { get { return Value.CodeGenerator; } }

        #region EmitToFloatingPoint

        private IEnumerable<IStorageLocation> EmitToFloatingPoint(IAssemblerEmitContext Context)
        {
            var fReg = Context.AllocateRegister(Type);
            var valReg = Value.EmitToRegister(Context);
            if (valReg.Type.get_IsBit())
            {
                Context.Emit(new Instruction(OpCodes.MoveToFloat32, new IInstructionArgument[] { Context.ToArgument(valReg), Context.ToArgument(fReg) }, "moves " + valReg.Identifier + " to " + fReg.Identifier));
            }
            else if (valReg.Type.get_IsInteger())
            {
                Context.Emit(new Instruction(OpCodes.MoveToFloat32, new IInstructionArgument[] { Context.ToArgument(valReg), Context.ToArgument(fReg) }, "moves " + valReg.Identifier + " to " + fReg.Identifier));
                Context.Emit(new Instruction(OpCodes.ConvertInt32ToFloat32, new IInstructionArgument[] { Context.ToArgument(fReg), Context.ToArgument(fReg) }, "converts value in " + fReg.Identifier + " to a floating-point number"));
            }
            else
            {
                throw new NotImplementedException();
            }
            valReg.EmitRelease().Emit(Context);
            return new IStorageLocation[] { fReg };
        }

        #endregion

        #region EmitFromFloatingPoint

        private IEnumerable<IStorageLocation> EmitFromFloatingPoint(IAssemblerEmitContext Context)
        {
            var fReg = Value.EmitToRegister(Context);
            var valReg = Context.AllocateRegister(Type);
            if (fReg.Type.GetSize() > 4)
            {
                if (valReg.Type.get_IsInteger())
                {
                    Context.Emit(new Instruction(OpCodes.ConvertFloat64ToInt32, new IInstructionArgument[] { Context.ToArgument(fReg), Context.ToArgument(fReg) }, "converts 64-bit floating-point number in " + fReg.Identifier + " to an integer"));
                    Context.Emit(new Instruction(OpCodes.MoveFromFloat32, new IInstructionArgument[] { Context.ToArgument(valReg), Context.ToArgument(fReg) }, "moves " + fReg.Identifier + " to " + valReg.Identifier));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                if (valReg.Type.get_IsBit())
                {
                    Context.Emit(new Instruction(OpCodes.MoveFromFloat32, new IInstructionArgument[] { Context.ToArgument(valReg), Context.ToArgument(fReg) }, "moves " + fReg.Identifier + " to " + valReg.Identifier));
                }
                else if (valReg.Type.get_IsInteger())
                {
                    Context.Emit(new Instruction(OpCodes.ConvertFloat32ToInt32, new IInstructionArgument[] { Context.ToArgument(fReg), Context.ToArgument(fReg) }, "converts floating-point number in " + fReg.Identifier + " to an integer"));
                    Context.Emit(new Instruction(OpCodes.MoveFromFloat32, new IInstructionArgument[] { Context.ToArgument(valReg), Context.ToArgument(fReg) }, "moves " + fReg.Identifier + " to " + valReg.Identifier));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            valReg.EmitRelease().Emit(Context);
            return new IStorageLocation[] { fReg };
        }

        #endregion

        #region EmitToBoolean

        private IEnumerable<IStorageLocation> EmitToBoolean(IAssemblerEmitContext Context)
        {
            var val = Value.EmitToRegister(Context);
            var endLabel = Context.DeclareLabel("end_is_zero");
            var elseLabel = Context.DeclareLabel("not_zero");
            var zeroRegister = Context.GetRegister(RegisterType.Zero, 0, Value.Type);
            Context.Emit(new Instruction(OpCodes.BranchNotEqual, new IInstructionArgument[] { Context.ToArgument(val), Context.ToArgument(zeroRegister), Context.ToArgument(endLabel) }));
            Context.Emit(new Instruction(OpCodes.Xor, new IInstructionArgument[] { Context.ToArgument(val), Context.ToArgument(val), Context.ToArgument(val) }, "'xors' the value with itself to obtain zero"));
            Context.Emit(new Instruction(OpCodes.Jump, Context.ToArgument(endLabel)));
            Context.MarkLabel(elseLabel);
            Context.Emit(new Instruction(OpCodes.AddImmediate, new IInstructionArgument[] { Context.ToArgument(val), Context.ToArgument(zeroRegister), Context.ToArgument(1) }, "adds '1' to $zero to obtain 1"));
            Context.MarkLabel(endLabel);
            return new IStorageLocation[] { val };
        }

        #endregion

        public IEnumerable<IStorageLocation> Emit(IAssemblerEmitContext Context)
        {
            var valType = Value.Type;
            if (valType.Equals(Type))
	        {
		        return Value.Emit(Context);
	        }
            else if (Type.Equals(PrimitiveTypes.Boolean))
            {
                return EmitToBoolean(Context);
            }
            else
            {
                if (Type.get_IsFloatingPoint())
                {
                    return EmitToFloatingPoint(Context);
                }
                else if (valType.get_IsFloatingPoint())
                {
                    return EmitFromFloatingPoint(Context);
                }
                else
                {
                    return new IStorageLocation[] { new RetypedStorageLocation(Value.Emit(Context).Single(), Type) };
                }
            }
        }
    }
}
