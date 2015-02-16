using Flame.Compiler;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class OpCodeInt8Block : OpCodeBlock
    {
        public OpCodeInt8Block(ICodeGenerator CodeGenerator, OpCode Value, sbyte Argument, IStackBehavior StackBehavior)
            : base(CodeGenerator, Value, StackBehavior)
        {
            this.Argument = Argument;
        }

        public sbyte Argument { get; private set; }

        public override void Emit(IEmitContext Context)
        {
            Context.Emit(Value, Argument);
            StackBehavior.Apply(Context.Stack);
        }
    }
    public class OpCodeInt16Block : OpCodeBlock
    {
        public OpCodeInt16Block(ICodeGenerator CodeGenerator, OpCode Value, short Argument, IStackBehavior StackBehavior)
            : base(CodeGenerator, Value, StackBehavior)
        {
            this.Argument = Argument;
        }

        public short Argument { get; private set; }

        public override void Emit(IEmitContext Context)
        {
            Context.Emit(Value, Argument);
            StackBehavior.Apply(Context.Stack);
        }
    }
    public class OpCodeInt32Block : OpCodeBlock
    {
        public OpCodeInt32Block(ICodeGenerator CodeGenerator, OpCode Value, int Argument, IStackBehavior StackBehavior)
            : base(CodeGenerator, Value, StackBehavior)
        {
            this.Argument = Argument;
        }

        public int Argument { get; private set; }

        public override void Emit(IEmitContext Context)
        {
            Context.Emit(Value, Argument);
            StackBehavior.Apply(Context.Stack);
        }
    }
    public class OpCodeInt64Block : OpCodeBlock
    {
        public OpCodeInt64Block(ICodeGenerator CodeGenerator, OpCode Value, long Argument, IStackBehavior StackBehavior)
            : base(CodeGenerator, Value, StackBehavior)
        {
            this.Argument = Argument;
        }

        public long Argument { get; private set; }

        public override void Emit(IEmitContext Context)
        {
            Context.Emit(Value, Argument);
            StackBehavior.Apply(Context.Stack);
        }
    }
    public class OpCodeFloat32Block : OpCodeBlock
    {
        public OpCodeFloat32Block(ICodeGenerator CodeGenerator, OpCode Value, float Argument, IStackBehavior StackBehavior)
            : base(CodeGenerator, Value, StackBehavior)
        {
            this.Argument = Argument;
        }

        public float Argument { get; private set; }

        public override void Emit(IEmitContext Context)
        {
            Context.Emit(Value, Argument);
            StackBehavior.Apply(Context.Stack);
        }
    }
    public class OpCodeFloat64Block : OpCodeBlock
    {
        public OpCodeFloat64Block(ICodeGenerator CodeGenerator, OpCode Value, double Argument, IStackBehavior StackBehavior)
            : base(CodeGenerator, Value, StackBehavior)
        {
            this.Argument = Argument;
        }

        public double Argument { get; private set; }

        public override void Emit(IEmitContext Context)
        {
            Context.Emit(Value, Argument);
            StackBehavior.Apply(Context.Stack);
        }
    }
    public class OpCodeStringBlock : OpCodeBlock
    {
        public OpCodeStringBlock(ICodeGenerator CodeGenerator, OpCode Value, string Argument, IStackBehavior StackBehavior)
            : base(CodeGenerator, Value, StackBehavior)
        {
            this.Argument = Argument;
        }

        public string Argument { get; private set; }

        public override void Emit(IEmitContext Context)
        {
            Context.Emit(Value, Argument);
            StackBehavior.Apply(Context.Stack);
        }
    }
    public class OpCodeTypeBlock : OpCodeBlock
    {
        public OpCodeTypeBlock(ICodeGenerator CodeGenerator, OpCode Value, IType Argument, IStackBehavior StackBehavior)
            : base(CodeGenerator, Value, StackBehavior)
        {
            this.Argument = Argument;
        }

        public IType Argument { get; private set; }

        public override void Emit(IEmitContext Context)
        {
            Context.Emit(Value, Argument);
            StackBehavior.Apply(Context.Stack);
        }
    }
}
