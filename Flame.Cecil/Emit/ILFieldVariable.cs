using Flame.Compiler;
using Flame.Compiler.Emit;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ILFieldVariable : IUnmanagedEmitVariable
    {
        public ILFieldVariable(ICodeGenerator CodeGenerator, ICecilBlock Target, IField Field)
        {
            this.CodeGenerator = CodeGenerator;
            this.Target = Target;
            this.Field = Field;
        }

        public ICodeGenerator CodeGenerator { get; private set; }
        public ICecilBlock Target { get; private set; }
        public IField Field { get; private set; }

        public IType Type
        {
            get { return Field.FieldType; }
        }

        public ICodeBlock EmitAddressOf()
        {
            return new FieldAddressOfBlock(this);
        }

        public ICodeBlock EmitGet()
        {
            return new FieldGetBlock(this);
        }

        public ICodeBlock EmitRelease()
        {
            return CodeGenerator.EmitVoid();
        }

        public ICodeBlock EmitSet(ICodeBlock Value)
        {
            return new FieldSetBlock(this, (ICecilBlock)Value);
        }
    }
}
