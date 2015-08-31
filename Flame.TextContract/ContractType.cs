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

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Template.GetAttributes();
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
                    foreach (var item in GetGenericParameters())
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

        public IType[] GetBaseTypes()
        {
            return Template.GetBaseTypes();
        }

        public IMethod[] GetConstructors()
        {
            return methodBuilders.Where((item) => item.IsConstructor).ToArray();
        }

        public IBoundObject GetDefaultValue()
        {
            return Template.GetDefaultValue();
        }

        public IField[] GetFields()
        {
            return fieldBuilders.ToArray();
        }

        public virtual IType GetGenericDeclaration()
        {
            return new ContractType(DeclaringNamespace, Template);
        }

        public ITypeMember[] GetMembers()
        {
            return methodBuilders.Concat<ITypeMember>(fieldBuilders).Concat(propertyBuilders).ToArray();
        }

        public IMethod[] GetMethods()
        {
            return methodBuilders.Where((item) => !item.IsConstructor).ToArray();
        }

        public IProperty[] GetProperties()
        {
            return propertyBuilders.ToArray();
        }

        public bool IsContainerType
        {
            get { return false; }
        }

        public IArrayType MakeArrayType(int Rank)
        {
            return new DescribedArrayType(this, Rank);
        }

        public IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            return new ContractGenericInstanceType(this, TypeArguments);
        }

        public IPointerType MakePointerType(PointerKind PointerKind)
        {
            return new DescribedPointerType(this, PointerKind);
        }

        public IVectorType MakeVectorType(int[] Dimensions)
        {
            return new DescribedVectorType(this, Dimensions);
        }

        public virtual IEnumerable<IType> GetGenericArguments()
        {
            return Template.GetGenericArguments();
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return Template.GenericParameters;
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.Append("class ");
            cb.Append(Name);
            var baseTypes = GetBaseTypes().Where((item) => !item.get_IsRootType()).ToArray();
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
    }
}
