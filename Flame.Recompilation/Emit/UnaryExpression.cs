﻿using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation.Emit
{
    public class UnaryExpression : IExpression
    {
        public UnaryExpression(IExpression Value, Operator Op)
        {
            this.Value = Value;
            this.Op = Op;
        }

        public IExpression Value { get; private set; }
        public Operator Op { get; private set; }

        public ICodeBlock Emit(ICodeGenerator Generator)
        {
            return Generator.EmitUnary(Value.Emit(Generator), Op);
        }

        public IBoundObject Evaluate()
        {
            return null;
        }

        public bool IsConstant
        {
            get { return false; }
        }

        public IExpression Optimize()
        {
            return this;
        }

        public IType Type
        {
            get
            {
                return Value.Type;
            }
        }
    }
}
