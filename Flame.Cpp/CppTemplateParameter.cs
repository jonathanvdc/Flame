using Flame.Build;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppTemplateParameter : IGenericParameter, ICppMember
    {
        public CppTemplateParameter(IGenericMember DeclaringMember, IGenericParameter Template, ICppEnvironment Environment)
        {
            this.DeclaringMember = DeclaringMember;
            this.Template = Template;
            this.Environment = Environment;
        }

        public IGenericMember DeclaringMember { get; private set; }
        public IGenericParameter Template { get; private set; }
        public ICppEnvironment Environment { get; private set; }

        public IGenericConstraint Constraint
        {
            get { return Template.Constraint; }
        }

        public INamespace DeclaringNamespace
        {
            get { return null; }
        }

        public IEnumerable<IType> BaseTypes
        {
            get { return Template.BaseTypes; }
        }

        public IBoundObject GetDefaultValue()
        {
            return null;
        }

        public IEnumerable<IField> Fields
        {
            get { return new IField[0]; }
        }

        public IEnumerable<IMethod> Methods
        {
            get { return new IMethod[0]; }
        }

        public IEnumerable<IProperty> Properties
        {
            get { return new IProperty[0]; }
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringMember.FullName, Name); }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return Template.Attributes; }
        }

        public string Name
        {
            get { return Template.Name; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Enumerable.Empty<IGenericParameter>(); }
        }

        #region ICppMember Implementation

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return BaseTypes.GetDependencies(this, DeclaringMember); }
        }

        public CodeBuilder GetHeaderCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("typename ");
            cb.Append(Name);
            return cb;
        }

        public bool HasSourceCode
        {
            get { return false; }
        }

        public CodeBuilder GetSourceCode()
        {
            return new CodeBuilder();
        }

        #endregion

        public IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
        }
    }
}
