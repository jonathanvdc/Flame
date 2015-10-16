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
        }

        public IRAssemblyBuilder Assembly { get; private set; }
        public FieldSignatureInstance Template { get; private set; }

        public void SetValue(IExpression Value)
        {
            this.InitialValueNode = new ConstantNodeStructure<IExpression>(IREmitHelpers.ConvertExpression(Assembly, Value, DeclaringType), Value);
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
