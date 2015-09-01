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
        public PythonParameter(string Name, IType ParameterType, params IAttribute[] Attributes)
            : this(Name, ParameterType, (IEnumerable<IAttribute>)Attributes)
        { }
        public PythonParameter(string Name, IType ParameterType, IEnumerable<IAttribute> Attributes)
            : this(Name, ParameterType, Attributes, null)
        { }
        public PythonParameter(string Name, IType ParameterType, IEnumerable<IAttribute> Attributes, IExpression DefaultValue)
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
        public string Name { get; private set; }
        public IEnumerable<IAttribute> Attributes { get; private set; }
        public IExpression DefaultValue { get; private set; }

        public string FullName
        {
            get { return Name; }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Attributes;
        }
    }
}
