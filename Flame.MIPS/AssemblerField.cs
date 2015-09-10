using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.MIPS.Emit;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Compiler.Build;

namespace Flame.MIPS
{
    public class AssemblerField : IAssemblerField, IFieldBuilder, IInitializedField
    {
        public AssemblerField(IType DeclaringType, IFieldSignatureTemplate Template, int Offset)
        {
            this.DeclaringType = DeclaringType;
            this.Template = new FieldSignatureInstance(Template, this);
            this.Offset = Offset;
        }

        public IType DeclaringType { get; private set; }
        public FieldSignatureInstance Template { get; private set; }
        public int Offset { get; private set; }
        public int Size { get { return FieldType.GetSize(); } }
        public IExpression InitialValue { get; private set; }

        public string Name { get { return Template.Name; } }
        public string FullName { get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); } }
        public bool IsStatic { get { return Template.IsStatic; } }
        public IType FieldType { get { return Template.FieldType.Value; } }
        public IEnumerable<IAttribute> Attributes
        {
            get { return Template.Attributes.Value; }
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
        
        public void Initialize()
        {
            if (this.InitialValue == null)
            {
                this.InitialValue = new DefaultValueExpression(FieldType);
            }            
        }
    }
}
