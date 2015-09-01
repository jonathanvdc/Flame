using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.MIPS.Emit;
using Flame.Compiler;
using Flame.Compiler.Expressions;

namespace Flame.MIPS
{
    public class AssemblerField : IAssemblerField, IFieldBuilder, IInitializedField
    {
        public AssemblerField(IType DeclaringType, IField Template, int Offset)
        {
            this.DeclaringType = DeclaringType;
            this.Template = Template;
            this.Offset = Offset;
            this.InitialValue = new DefaultValueExpression(Template.FieldType);
        }

        public IType DeclaringType { get; private set; }
        public IField Template { get; private set; }
        public int Offset { get; private set; }
        public int Size { get { return FieldType.GetSize(); } }
        public IExpression InitialValue { get; private set; }

        public string Name { get { return Template.Name; } }
        public string FullName { get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); } }
        public bool IsStatic { get { return Template.IsStatic; } }
        public IType FieldType { get { return Template.FieldType; } }
        public IEnumerable<IAttribute> GetAttributes()
        {
            return Template.Attributes;
        }

        public void SetValue(IExpression Value)
        {
            this.InitialValue = Value;
        }

        public IExpression GetValue()
        {
            return this.InitialValue;
        }

        public IField Build()
        {
            return this;
        }
    }
}
