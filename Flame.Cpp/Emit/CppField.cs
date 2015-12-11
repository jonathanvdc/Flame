using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class CppField : CppVariableBase
    {
        public CppField(ICodeGenerator CodeGenerator, ICppBlock Target, IField Field)
            : base(CodeGenerator)
        {
            this.Target = Target;
            this.Field = Field;
        }

        public ICppBlock Target { get; private set; }
        public IField Field { get; private set; }

        public override ICppBlock CreateBlock()
        {
            if (Target != null)
            {
                return new MemberAccessBlock(Target, Field, Type);
            }
            else
            {
                return Field.CreateBlock(CodeGenerator);
            }
        }

        public override ICodeBlock EmitSet(ICodeBlock Value)
        {
            if (Field is Flame.Cpp.CppField && CodeGenerator.Method.GetIsConstant() && !CodeGenerator.Method.IsConstructor)
            {
                var cppField = (Flame.Cpp.CppField)Field;
                cppField.IsMutable = true;
            }
            return base.EmitSet(Value);
        }

        public override IType Type
        {
            get { return Field.FieldType; }
        }
    }
}
