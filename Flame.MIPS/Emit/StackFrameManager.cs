using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Emit
{
    public class StackFrameManager : IFrameManager
    {
        public StackFrameManager(ICodeGenerator CodeGenerator)
        {
            this.CodeGenerator = CodeGenerator;

            this.preservedTemporaries = new List<RegisterData>();
            this.stackLocals = new List<OffsetRegisterLocation>();
            this.preservedLocals = new List<RegisterData>();
            this.argumentMapping = new List<IStorageLocation>();
        }

        public ICodeGenerator CodeGenerator { get; private set; }

        public IRegister FramePointer { get; private set; }
        public IRegister StackPointer { get; private set; }
        public IRegister AddressRegister { get; private set; }

        private List<RegisterData> preservedLocals;
        private List<OffsetRegisterLocation> stackLocals;
        private List<IStorageLocation> argumentMapping;
        private List<RegisterData> preservedTemporaries;

        public void Bind(IAssemblerEmitContext Context)
        {
            this.FramePointer = Context.GetRegister(RegisterType.FramePointer, 0, PrimitiveTypes.Int32);
            this.StackPointer = Context.GetRegister(RegisterType.StackPointer, 0, PrimitiveTypes.Int32);
            this.AddressRegister = Context.GetRegister(RegisterType.AddressRegister, 0, PrimitiveTypes.Int32);
            // Setup arguments
            foreach (var item in CodeGenerator.Method.GetParameters())
            {
                var target = Context.AllocateLocal(item.ParameterType);
                argumentMapping.Add(target);
            }
        }

        #region Frame Layout

        public OffsetRegisterLocation DynamicLink
        {
            get
            {
                return new OffsetRegisterLocation(CodeGenerator, FramePointer, 0, PrimitiveTypes.Int32);
            }
        }

        public OffsetRegisterLocation ReturnAddress
        {
            get
            {
                return new OffsetRegisterLocation(CodeGenerator, FramePointer, -4, PrimitiveTypes.Int32);
            }
        }

        public OffsetRegisterLocation StaticLink
        {
            get
            {
                return new OffsetRegisterLocation(CodeGenerator, FramePointer, -8, PrimitiveTypes.Int32);
            }
        }

        public int FrameSize
        {
            get
            {
                // Dynamic link + Return address + Static link + caller locals + stack locals
                if (stackLocals.Count > 0)
                {
                    var last = stackLocals[stackLocals.Count - 1];
                    return (int)-last.Offset + last.Type.GetSize();
                }
                else if (preservedLocals.Count > 0)
                {
                    return 12 + preservedLocals.Count * 4;
                }
                else
                {
                    return 12;
                }
            }
        }

        public IUnmanagedStorageLocation StackAllocate(IType Type)
        {
            var location = new OffsetRegisterLocation(CodeGenerator, FramePointer, -FrameSize, Type);
            stackLocals.Add(location);
            return location;
        }

        #endregion

        #region Arguments

        public IStorageLocation GetArgument(IAssemblerEmitContext Context, int Index)
        {
            //return Context.GetRegister(RegisterType.Argument, Index, CodeGenerator.Method.GetParameters()[Index].ParameterType);
            return ManualReleaseLocation.Create(CodeGenerator, argumentMapping[Index]);
        }

        #endregion

        #region State Preservation

        public void PreserveRegister(RegisterData Register)
        {
            if (Register.Kind == RegisterType.Local) // Locals are preserved differently 
            {
                if (!preservedLocals.Contains(Register))
                {
                    preservedLocals.Add(Register);
                }
            }
            else
            {
                if (!preservedTemporaries.Contains(Register))
                {
                    preservedTemporaries.Add(Register);
                }
            }
        }

        public void ReleaseRegister(RegisterData Register)
        {
            preservedTemporaries.Remove(Register);
        }

        #endregion

        #region Frame Setup

        public void EmitInitializeFrame(IAssemblerEmitContext Context)
        {
            // sw	$fp, ($sp)	                    # push old frame pointer (dynamic link)
            // move $fp, $sp                        # frame	pointer now points to the top of the stack
            // subu $sp, $sp, <stack frame size>    # allocate <stack frame size> bytes on the stack
            // sw   $ra, -4($fp)                    # store the value of the return address
            // sw   $v0, -8($fp)                    # save the static link

            // sw   $s0, -12($fp)                   # save locally used registers
            // sw   $s1, -16($fp)


            // move $s0, $a0                        # $s0 = first number to be added
            // move $s1, $a1                        # $s1 = second number to be added

            Context.Emit(new Instruction(OpCodes.StoreInt32, new IInstructionArgument[] { Context.ToArgument(FramePointer), Context.ToArgument(0, StackPointer) }, "push old frame pointer (dynamic link)"));
            Context.Emit(new Instruction(OpCodes.Move, new IInstructionArgument[] { Context.ToArgument(FramePointer), Context.ToArgument(StackPointer) }, "frame pointer now points to the top of the stack"));
            int fsize = FrameSize;
            Context.Emit(new Instruction(OpCodes.SubtractUnsigned, new IInstructionArgument[] { Context.ToArgument(StackPointer), Context.ToArgument(StackPointer), Context.ToArgument(fsize) }, "allocate " + fsize + " bytes on the stack"));
            Context.Emit(new Instruction(OpCodes.StoreInt32, new IInstructionArgument[] { Context.ToArgument(AddressRegister), Context.ToArgument(-4, FramePointer) }, "store the value of the return address"));
            Context.Emit(new Instruction(OpCodes.StoreInt32, new IInstructionArgument[] { Context.ToArgument(Context.GetRegister(RegisterType.ReturnValue, 0, PrimitiveTypes.Int32)), Context.ToArgument(-8, FramePointer) }, "store the static link"));

            Context.EmitEmptyLine();

            int offset = 12;
            if (preservedLocals.Count > 0)
            {
                Context.Emit(new Instruction(OpCodes.StoreInt32, new IInstructionArgument[] { Context.ToArgument(Context.GetRegister(preservedLocals[0].Kind, preservedLocals[0].Index, PrimitiveTypes.Int32)), Context.ToArgument(-offset, FramePointer) }, "save locally used registers"));
                offset += 4;
                foreach (var item in preservedLocals.Skip(1))
                {
                    Context.Emit(new Instruction(OpCodes.StoreInt32, new IInstructionArgument[] { Context.ToArgument(Context.GetRegister(item.Kind, item.Index, PrimitiveTypes.Int32)), Context.ToArgument(-offset, FramePointer) }));
                    offset += 4;
                }
                Context.EmitEmptyLine();
            }

            var parameters = CodeGenerator.Method.GetParameters();
            if (argumentMapping.Count > 0)
            {
                Context.EmitComment("moves arguments to locals");
                for (int i = 0; i < argumentMapping.Count; i++)
                {
                    Context.EmitComment(parameters[i].Name + " --> " + (argumentMapping[i] is IRegister ? ((IRegister)argumentMapping[i]).Identifier : "stack"));
                    argumentMapping[i].EmitStore(Context.GetRegister(RegisterType.Argument, i, argumentMapping[i].Type)).Emit(Context);
                }
                Context.EmitEmptyLine();
            }
        }

        #endregion

        #region Return

        public void EmitReturnInstructions(IEnumerable<IStorageLocation> ReturnValues, IAssemblerEmitContext Context)
        {
            // move	$v0, $t0        # place result in return value location

            // lw	$s1, -16($fp)	# reset saved register $s1
            // lw	$s0, -12($fp)	# reset saved register $s0
            // lw	$ra, -4($fp)    # get return address from frame
            // move	$sp, $fp        # get old frame pointer from current frame
            // lw	$fp, ($sp)	    # restore old frame pointer
            // jr	$ra

            var retVals = ReturnValues.ToArray();
            if (retVals.Length > 2)
            {
                throw new NotSupportedException("More than two return values are not supported.");
            }

            if (retVals.Length > 0)
            {
                Context.EmitComment("place result" + (retVals.Length == 1 ? "" : "s") + " in return value location");
                for (int i = 0; i < retVals.Length; i++)
                {
                    retVals[i].EmitLoad(Context.GetRegister(RegisterType.ReturnValue, i, retVals[i].Type)).Emit(Context);
                }
                Context.EmitEmptyLine();
            }

            int offset = 12 + preservedLocals.Count * 4;

            foreach (var item in Enumerable.Reverse(preservedLocals))
            {
                Context.Emit(new Instruction(OpCodes.LoadInt32, new IInstructionArgument[] { Context.ToArgument(Context.GetRegister(item.Kind, item.Index, PrimitiveTypes.Int32)), Context.ToArgument(-offset, FramePointer) }, "reset saved register " + item.ToString()));
                offset -= 4;
            }

            Context.Emit(new Instruction(OpCodes.LoadInt32, new IInstructionArgument[] { Context.ToArgument(AddressRegister), Context.ToArgument(-4, FramePointer) }, "get return address from frame"));
            Context.Emit(new Instruction(OpCodes.Move, new IInstructionArgument[] { Context.ToArgument(StackPointer), Context.ToArgument(FramePointer) }, "get old frame pointer from current frame"));
            Context.Emit(new Instruction(OpCodes.LoadInt32, new IInstructionArgument[] { Context.ToArgument(FramePointer), Context.ToArgument(0, StackPointer) }, "restore old frame pointer"));
            Context.Emit(new Instruction(OpCodes.JumpRegister, new IInstructionArgument[] { Context.ToArgument(AddressRegister) }, "jumps back to caller"));
        }

        #endregion

        #region Invocations

        protected IEnumerable<IStorageLocation> EmitInvokeInstructions(IAssemblerBlock Call, ICallConvention CallConvention, IEnumerable<IStorageLocation> Arguments, IAssemblerEmitContext Context)
        {
            int delta = CallConvention.StackDelta;
            if (delta > 0)
            {
                Context.EmitEmptyLine();
                Context.Emit(new Instruction(OpCodes.SubtractUnsigned, new IInstructionArgument[] { Context.ToArgument(StackPointer), Context.ToArgument(StackPointer), Context.ToArgument(delta) }, "allocate stack storage for arguments and return values"));
            }
            var convArgs = CallConvention.GetArguments(Context);
            if (convArgs.Any())
            {
                Context.EmitComment("moves argument values into argument locations");
                var zipped = Arguments.Zip(convArgs, (a, b) => new KeyValuePair<IStorageLocation, IStorageLocation>(a, b));
                foreach (var item in zipped)
                {
                    item.Key.ReleaseTo(item.Value, Context);
                }
                Context.EmitEmptyLine();
            }
            if (CallConvention.UsesStaticLink)
            {
                Context.EmitComment("passes static link through $v0");
                var v0 = Context.AcquireRegister(RegisterType.ReturnValue, 0, PrimitiveTypes.Int32);
                if (CallConvention.Method.Equals(CodeGenerator.Method))
                {
                    StaticLink.EmitLoad(v0).Emit(Context);
                }
                else
                {
                    FramePointer.EmitLoad(v0).Emit(Context);
                }
                v0.EmitRelease().Emit(Context);
                Context.EmitEmptyLine();
            }
            var retVals = CallConvention.GetReturnValues(Context);
            Call.Emit(Context);
            Context.EmitEmptyLine();
            foreach (var item in convArgs)
            {
                item.EmitRelease().Emit(Context);
            }
            List<IStorageLocation> results = new List<IStorageLocation>();
            foreach (var item in retVals)
            {
                results.Add(item.SpillRegister(Context));
            }
            if (delta > 0)
            {
                Context.EmitEmptyLine();
                Context.Emit(new Instruction(OpCodes.AddImmediateUnsigned, new IInstructionArgument[] { Context.ToArgument(StackPointer), Context.ToArgument(StackPointer), Context.ToArgument(delta) }, "free stack storage for arguments and return values"));
            }
            return results;
        }
        
        public IEnumerable<IStorageLocation> EmitInvokeInstructions(IMethod Method, IEnumerable<IStorageLocation> Arguments, IAssemblerEmitContext Context)
        {
            var asmMethod = (IAssemblerMethod)Method;
            var convention = asmMethod.CallConvention;
            var jumpBlock = asmMethod.CreateCallBlock(CodeGenerator);
            return EmitInvokeInstructions(jumpBlock, convention, Arguments, Context);
        }

        public IEnumerable<IStorageLocation> EmitInvokeInstructions(IRegister Method, ICallConvention CallConvention, IEnumerable<IStorageLocation> Arguments, IAssemblerEmitContext Context)
        {
            var callBlock = new CallRegisterBlock(CodeGenerator, Method);
            var results = EmitInvokeInstructions(callBlock, CallConvention, Arguments, Context);

            return results;
        }

        #endregion
    }
}
