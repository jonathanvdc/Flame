using Flame.Build;
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
            this.templParams = new Lazy<List<CppTemplateParameter>>(() => new List<CppTemplateParameter>());
        }
        public CppTemplateDefinition(ICppTemplateMember DeclaringMember, Lazy<IEnumerable<IGenericParameter>> TemplateParameters)
        {
            this.DeclaringMember = DeclaringMember;
            this.templParams = new Lazy<List<CppTemplateParameter>>(() => 
                new List<CppTemplateParameter>(TemplateParameters.Value.Select(item =>
                    new CppTemplateParameter(DeclaringMember, item, Environment))));
        }

        private Lazy<List<CppTemplateParameter>> templParams;

        public bool IsEmpty
        {
            get
            {
                return templParams.Value.Count == 0;
            }
        }

        private void AddTemplateParameter(CppTemplateParameter Parameter)
        {
            this.templParams.Value.Add(Parameter);
        }

        public IGenericParameter DeclareGenericParameter(IGenericParameter Template)
        {
            var parameter = new CppTemplateParameter(DeclaringMember, Template, Environment);
            AddTemplateParameter(parameter);
            return parameter;
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return templParams.Value;
        }

        public ICppTemplateMember DeclaringMember { get; private set; }
        public ICppEnvironment Environment { get { return DeclaringMember.Environment; } }

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return templParams.Value.GetDependencies(); }
        }

        public CodeBuilder GetImplementationCode()
        {
            if (IsEmpty)
            {
                return new CodeBuilder("template<>");
            }
            else
            {
                return GetHeaderCode();
            }
        }

        public CodeBuilder GetHeaderCode()
        {
            if (IsEmpty)
            {
                return new CodeBuilder();
            }
            CodeBuilder cb = new CodeBuilder();
            cb.Append("template<");
            cb.Append(templParams.Value[0].GetHeaderCode());
            foreach (var item in templParams.Value.Skip(1))
            {
                cb.Append(", ");
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
            foreach (var item in this.templParams.Value.Concat(Other.templParams.Value))
            {
                newDef.AddTemplateParameter(item);
            }
            return newDef;
        }
    }
}
