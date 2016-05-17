using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public class ContractType : ITypeBuilder, ISyntaxNode
    {
        public ContractType(INamespace DeclaringNamespace, ITypeSignatureTemplate Template)
        {
            this.DeclaringNamespace = DeclaringNamespace;
            this.Template = new TypeSignatureInstance(Template, this);
            this.fieldBuilders = new List<ContractField>();
            this.methodBuilders = new List<ContractMethod>();
            this.propertyBuilders = new List<ContractProperty>();
        }

        public TypeSignatureInstance Template { get; private set; }
        public INamespace DeclaringNamespace { get; private set; }

        private List<ContractField> fieldBuilders;
        private List<ContractMethod> methodBuilders;
        private List<ContractProperty> propertyBuilders;

        public IFieldBuilder DeclareField(IFieldSignatureTemplate Template)
        {
            var field = new ContractField(this, Template);
            fieldBuilders.Add(field);
            return field;
        }

        public IMethodBuilder DeclareMethod(IMethodSignatureTemplate Template)
        {
            var method = new ContractMethod(this, Template);
            methodBuilders.Add(method);
            return method;
        }

        public IPropertyBuilder DeclareProperty(IPropertySignatureTemplate Template)
        {
            var property = new ContractProperty(this, Template);
            propertyBuilders.Add(property);
            return property;
        }

        public IType Build()
        {
            return this;
        }

        public void Initialize()
        {
            // Do nothing. This back-end does not need `Initialize` to get things done.
        }

        public QualifiedName FullName
        {
            get { return Name.Qualify(DeclaringNamespace.FullName); }
        }

        public AttributeMap Attributes
        {
            get { return Template.Attributes.Value; }
        }

        public virtual UnqualifiedName Name
        {
            get
            {
                return Template.Name;
            }
        }

        public IEnumerable<IType> BaseTypes
        {
            get { return Template.BaseTypes.Value; }
        }

        public IBoundObject GetDefaultValue()
        {
            return null;
        }

        public IEnumerable<IField> Fields
        {
            get { return fieldBuilders; }
        }

        public IEnumerable<IMethod> Methods
        {
            get { return methodBuilders; }
        }

        public IEnumerable<IProperty> Properties
        {
            get { return propertyBuilders; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Template.GenericParameters.Value; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("class ");
            cb.Append(this.MakeGenericType(this.GenericParameters).Name.ToString());
            var baseTypes = BaseTypes.Where((item) => !item.GetIsRootType()).ToArray();
            if (baseTypes.Length > 0)
            {
                cb.Append(" : ");
                for (int i = 0; i < baseTypes.Length; i++)
                {
                    if (i > 0)
                    {
                        cb.Append(", ");
                    }
                    cb.Append(ContractHelpers.GetTypeName(baseTypes[i]));
                }
            }
            cb.AddCodeBuilder(this.GetDocumentationCode());
            cb.AddLine("{");
            cb.IncreaseIndentation();
            cb.AddEmptyLine();

            foreach (var item in methodBuilders.Where((item) => item.IsConstructor).Where(ContractHelpers.InContract))
            {
                cb.AddCodeBuilder(item.GetCode());
                cb.AddEmptyLine();
            }

            foreach (var item in propertyBuilders.Where(ContractHelpers.InContract))
            {
                cb.AddCodeBuilder(item.GetCode());
                cb.AddEmptyLine();
            }

            foreach (var item in methodBuilders.Where((item) => !item.IsConstructor).Where(ContractHelpers.InContract))
            {
                cb.AddCodeBuilder(item.GetCode());
                cb.AddEmptyLine();
            }

            cb.DecreaseIndentation();
            cb.AddLine("}");

            return cb;
        }

        public IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
        }
    }
}
