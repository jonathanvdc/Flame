using Flame.Compiler;
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
        public ContractProperty(IType DeclaringType, IProperty Template)
        {
            this.DeclaringType = DeclaringType;
            this.Template = Template;
            this.accessors = new List<ContractAccessor>();
        }

        public IProperty Template { get; private set; }
        public IType DeclaringType { get; private set; }

        private List<ContractAccessor> accessors;

        public IMethodBuilder DeclareAccessor(IAccessor Template)
        {
            var method = new ContractAccessor(this, Template);
            accessors.Add(method);
            return method;
        }

        public IProperty Build()
        {
            return this;
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Template.GetAttributes();
        }

        public string Name
        {
            get 
            {
                return Template.Name; 
            }
        }

        public IAccessor[] GetAccessors()
        {
            return accessors.ToArray();
        }

        public IParameter[] GetIndexerParameters()
        {
            return Template.GetIndexerParameters();
        }

        public IType PropertyType
        {
            get { return Template.PropertyType; }
        }

        public bool IsStatic
        {
            get { return Template.IsStatic; }
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            if (Template.get_IsIndexer() && Template.Name == "this")
            {
                cb.Append("indexer");
            }
            else
            {
                cb.Append("property ");
                cb.Append(Name);
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
