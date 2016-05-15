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
    public class ContractProperty : IPropertyBuilder, ISyntaxNode
    {
        public ContractProperty(IType DeclaringType, IPropertySignatureTemplate Template)
        {
            this.DeclaringType = DeclaringType;
            this.Template = new PropertySignatureInstance(Template, this);
            this.accessors = new List<ContractAccessor>();
        }

        public PropertySignatureInstance Template { get; private set; }
        public IType DeclaringType { get; private set; }

        private List<ContractAccessor> accessors;

        public IMethodBuilder DeclareAccessor(AccessorType Type, IMethodSignatureTemplate Template)
        {
            var method = new ContractAccessor(this, Type, Template);
            accessors.Add(method);
            return method;
        }

        public IProperty Build()
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
            get 
            {
                return Template.Name; 
            }
        }

        public IEnumerable<IAccessor> Accessors
        {
            get { return accessors; }
        }

        public IEnumerable<IParameter> IndexerParameters
        {
            get { return Template.IndexerParameters.Value; }
        }

        public IType PropertyType
        {
            get { return Template.PropertyType.Value; }
        }

        public bool IsStatic
        {
            get { return Template.Template.IsStatic; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            if (this.GetIsIndexer())
            {
                cb.Append("indexer");
            }
            else
            {
                cb.Append("property ");
                cb.Append(Name.ToString());
            }
            cb.AddLine("{");
            cb.IncreaseIndentation();
            cb.AddEmptyLine();
            foreach (var item in accessors.Where(ContractHelpers.InContract))
            {
                cb.AddCodeBuilder(item.GetCode());
                cb.AddEmptyLine();
            }
            cb.DecreaseIndentation();
            cb.AddLine("}");
            return cb;
        }
    }
}
