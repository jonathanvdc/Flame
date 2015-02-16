using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class CppTemplateDefinition
    {
        public CppTemplateDefinition(ICppTemplateMember DeclaringMember)
        {
            this.DeclaringMember = DeclaringMember;
            this.templParams = new List<CppTemplateParameter>();
        }
        public CppTemplateDefinition(ICppTemplateMember DeclaringMember, IGenericMember Template)
            : this(DeclaringMember)
        {
            DeclareGenericParameters(Template);
        }

        private List<CppTemplateParameter> templParams;

        public bool IsEmpty
        {
            get
            {
                return templParams.Count == 0;
            }
        }

        private void AddTemplateParameter(CppTemplateParameter Parameter)
        {
            this.templParams.Add(Parameter);
        }

        public IGenericParameter DeclareGenericParameter(IGenericParameter Template)
        {
            var parameter = new CppTemplateParameter(DeclaringMember, Template, Environment);
            AddTemplateParameter(parameter);
            return parameter;
        }

        public IEnumerable<IGenericParameter> DeclareGenericParameters(IGenericMember Template)
        {
            return Template.GetGenericParameters().Select(DeclareGenericParameter).ToArray();
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return templParams;
        }

        public ICppTemplateMember DeclaringMember { get; private set; }
        public ICppEnvironment Environment { get { return DeclaringMember.Environment; } }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return templParams.GetDependencies(); }
        }

        public CodeBuilder GetHeaderCode()
        {
            if (IsEmpty)
            {
                return new CodeBuilder();
            }
            CodeBuilder cb = new CodeBuilder();
            cb.Append("template<");
            foreach (var item in templParams)
            {
                cb.Append(item.GetHeaderCode());
            }
            cb.Append(">");
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

        public CppTemplateDefinition Merge(CppTemplateDefinition Other)
        {
            CppTemplateDefinition newDef = new CppTemplateDefinition(Other.DeclaringMember);
            foreach (var item in this.templParams.Concat(Other.templParams))
            {
                newDef.AddTemplateParameter(item);
            }
            return newDef;
        }
    }
}
