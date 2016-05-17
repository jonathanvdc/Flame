using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonParameter : IParameter
    {
        public PythonParameter(IParameter Template)
            : this(Template.Name, Template.ParameterType, Template.Attributes)
        { }
        public PythonParameter(IParameter Template, IExpression DefaultValue)
            : this(Template.Name, Template.ParameterType, Template.Attributes, DefaultValue)
        { }
        public PythonParameter(UnqualifiedName Name, IType ParameterType)
            : this(Name, ParameterType, AttributeMap.Empty)
        { }
        public PythonParameter(UnqualifiedName Name, IType ParameterType, AttributeMap Attributes)
            : this(Name, ParameterType, Attributes, null)
        { }
        public PythonParameter(UnqualifiedName Name, IType ParameterType, AttributeMap Attributes, IExpression DefaultValue)
        {
            this.Name = Name;
            this.ParameterType = ParameterType;
            this.Attributes = Attributes;
            this.DefaultValue = DefaultValue;
        }

        public bool IsAssignable(IType Type)
        {
            return Type.Is(ParameterType);
        }

        public IType ParameterType { get; private set; }
        public UnqualifiedName Name { get; private set; }
        public AttributeMap Attributes { get; private set; }
        public IExpression DefaultValue { get; private set; }

        public QualifiedName FullName
        {
            get { return new QualifiedName(Name); }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Attributes;
        }
    }
}
