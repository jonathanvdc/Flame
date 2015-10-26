using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRFieldBuilder : IRField, IFieldBuilder
    {
        public IRFieldBuilder(IRAssemblyBuilder Assembly, IType DeclaringType, IFieldSignatureTemplate Template)
            : base(DeclaringType, new IRSignature(Template.Name), Template.IsStatic)
        {
            this.Assembly = Assembly;
            this.Template = new FieldSignatureInstance(Template, this);
            // Set the field type node to null.
            // This will result in a NullReferenceException if
            // it is accessed before it is initialized.
            // This is useful for debugging, as neglecting to do this
            // will make the type report incorrect (but seemingly valid)
            // values for the field type property.
            this.FieldTypeNode = null;
        }

        public IRAssemblyBuilder Assembly { get; private set; }
        public FieldSignatureInstance Template { get; private set; }

        public void SetValue(IExpression Value)
        {
            this.InitialValueNode = new LazyNodeStructure<IExpression>(Value, () => IREmitHelpers.ConvertExpression(Assembly, Value, DeclaringType));
        }

        public IField Build()
        {
            return this;
        }

        public void Initialize()
        {
            this.Signature = IREmitHelpers.CreateSignature(Assembly, Template.Name, Template.Attributes.Value);
            this.FieldTypeNode = Assembly.TypeTable.GetReferenceStructure(Template.FieldType.Value);
        }
    }
}
