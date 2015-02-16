using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Statements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil.Emit
{
    public class ILFieldVariable : IUnmanagedVariable
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

        public IExpression CreateAddressOfExpression()
        {
            return new CodeBlockExpression(new FieldAddressOfBlock(this), Type.MakePointerType(PointerKind.ReferencePointer));
        }

        public IExpression CreateGetExpression()
        {
            return new CodeBlockExpression(new FieldGetBlock(this), Type);
        }

        public IStatement CreateReleaseStatement()
        {
            return new EmptyStatement();
        }

        public IStatement CreateSetStatement(IExpression Value)
        {
            return new CodeBlockStatement(new FieldSetBlock(this, (ICecilBlock)Value.Emit(CodeGenerator)));
        }

        public IType Type
        {
            get { return Field.FieldType; }
        }
    }
}
