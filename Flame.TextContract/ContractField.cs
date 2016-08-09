using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public class ContractField : IFieldBuilder
    {
        public ContractField(IType DeclaringType, IFieldSignatureTemplate Template)
        {
            this.DeclaringType = DeclaringType;
            this.Template = new FieldSignatureInstance(Template, this);
        }

        public FieldSignatureInstance Template { get; private set; }
        public IType DeclaringType { get; private set; }

        public bool TrySetValue(IExpression Value)
        {
            return false;
        }

        public IField Build()
        {
            return this;
        }

        public void Initialize()
        {
            // Do nothing. This back-end does not need `Initialize` to get things done.
        }

        public QualifiedName FullName
        {
            get { return Name.Qualify(DeclaringType.FullName); }
        }

        public AttributeMap Attributes
        {
            get { return Template.Attributes.Value; }
        }

        public UnqualifiedName Name
        {
            get { return Template.Name; }
        }

        public IType FieldType
        {
            get { return Template.FieldType.Value; }
        }

        public bool IsStatic
        {
            get { return Template.IsStatic; }
        }
    }
}
