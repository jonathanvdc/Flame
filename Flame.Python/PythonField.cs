using Flame.Compiler;
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
        public PythonField(IType DeclaringType, IField Template)
        {
            this.DeclaringType = DeclaringType;
            this.Template = Template;
        }

        public IType DeclaringType { get; private set; }
        public IField Template { get; private set; }

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
        public void SetValue(IExpression Value)
        {
            this.assignedVal = Value;
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
            return Template.Attributes;
        }

        protected IMemberNamer GetMemberNamer()
        {
            return DeclaringType.DeclaringNamespace.GetMemberNamer();
        }

        public string Name
        {
            get { return DeclaringType.DeclaringNamespace.GetMemberNamer().Name(Template); }
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
