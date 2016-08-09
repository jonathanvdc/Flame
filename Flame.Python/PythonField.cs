using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonField : IFieldBuilder
    {
        public PythonField(IType DeclaringType, IFieldSignatureTemplate Template)
        {
            this.DeclaringType = DeclaringType;
            this.Template = new FieldSignatureInstance(Template, this);
        }

        public IType DeclaringType { get; private set; }
        public FieldSignatureInstance Template { get; private set; }

        private IExpression assignedVal;
        public IExpression AssignedValue
        {
            get
            {
                if (assignedVal == null)
                {
                    assignedVal = new DefaultValueExpression(FieldType);
                }
                return assignedVal;
            }
        }
        public bool TrySetValue(IExpression Value)
        {
            this.assignedVal = Value;
            return true;
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

        protected IMemberNamer GetMemberNamer()
        {
            return DeclaringType.DeclaringNamespace.GetMemberNamer();
        }

        public UnqualifiedName Name
        {
            get
            {
                var descField = new DescribedField(Template.Name, DeclaringType, FieldType);
                return new SimpleName(DeclaringType.DeclaringNamespace.GetMemberNamer().Name(descField));
            }
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
