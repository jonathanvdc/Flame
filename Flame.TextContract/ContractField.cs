using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public class ContractField : IFieldBuilder
    {
        public ContractField(IType DeclaringType, IField Template)
        {
            this.DeclaringType = DeclaringType;
            this.Template = Template;
        }

        public IField Template { get; private set; }
        public IType DeclaringType { get; private set; }

        public void SetValue(IExpression Value)
        {
            throw new NotImplementedException();
        }

        public IField Build()
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
            get { return Template.Name; }
        }

        public IType FieldType
        {
            get { return Template.FieldType; }
        }

        public bool IsStatic
        {
            get { return Template.IsStatic; }
        }
    }
}
