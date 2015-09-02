using Flame.Build;
using Flame.Compiler;
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
        public ContractType(INamespace DeclaringNamespace, IType Template)
        {
            this.DeclaringNamespace = DeclaringNamespace;
            this.Template = Template;
            this.fieldBuilders = new List<ContractField>();
            this.methodBuilders = new List<ContractMethod>();
            this.propertyBuilders = new List<ContractProperty>();
        }

        public IType Template { get; private set; }
        public INamespace DeclaringNamespace { get; private set; }

        private List<ContractField> fieldBuilders;
        private List<ContractMethod> methodBuilders;
        private List<ContractProperty> propertyBuilders;

        public IFieldBuilder DeclareField(IField Template)
        {
            var field = new ContractField(this, Template);
            fieldBuilders.Add(field);
            return field;
        }

        public IMethodBuilder DeclareMethod(IMethod Template)
        {
            var method = new ContractMethod(this, Template);
            methodBuilders.Add(method);
            return method;
        }

        public IPropertyBuilder DeclareProperty(IProperty Template)
        {
            var property = new ContractProperty(this, Template);
            propertyBuilders.Add(property);
            return property;
        }

        public IType Build()
        {
            return this;
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return Template.Attributes; }
        }

        public virtual string Name
        {
            get
            {
                if (this.get_IsGeneric())
                {
                    var genericFreeName = this.Template.GetGenericDeclaration().GetGenericFreeName();
                    StringBuilder sb = new StringBuilder(genericFreeName);
                    sb.Append('<');
                    bool first = true;
                    foreach (var item in GenericParameters)
                    {
                        if (first)
                        {
                            first = false;
                        }
                        else
                        {
                            sb.Append(", ");
                        }
                        sb.Append(ContractHelpers.GetTypeName(item));
                    }
                    sb.Append('>');
                    return sb.ToString();
                }
                else
                {
                    return Template.Name;
                }
            }
        }

        public IContainerType AsContainerType()
        {
            return null;
        }

        public IEnumerable<IType> BaseTypes
        {
            get { return Template.BaseTypes; }
        }

        public IBoundObject GetDefaultValue()
        {
            return Template.GetDefaultValue();
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
            get { return Template.GenericParameters; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("class ");
            cb.Append(Name);
            var baseTypes = BaseTypes.Where((item) => !item.get_IsRootType()).ToArray();
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
