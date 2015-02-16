using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Expressions;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonClass : ITypeBuilder, ISyntaxNode, IDependencyNode
    {
        public PythonClass(IType Template)
        {
            this.Template = Template;
            this.ctors = new List<IPythonMethod>();
            this.fields = new List<IField>();
            this.methods = new List<IPythonMethod>();
            this.properties = new List<PythonProperty>();
        }

        public IType Template { get; private set; }

        #region IType Implementation

        public IContainerType AsContainerType()
        {
            return null;
        }

        public INamespace DeclaringNamespace
        {
            get { return Template.DeclaringNamespace; }
        }

        public IType[] GetBaseTypes()
        {
            return Template.GetBaseTypes();
        }

        private List<IPythonMethod> ctors;
        private List<IField> fields;
        private List<IPythonMethod> methods;
        private List<PythonProperty> properties;

        public IMethod[] GetConstructors()
        {
            return ctors.ToArray();
        }

        public IBoundObject GetDefaultValue()
        {
            return new NullExpression();
        }

        public IField[] GetFields()
        {
            return fields.ToArray();
        }

        public IType GetGenericDeclaration()
        {
            return this;
        }

        public ITypeMember[] GetMembers()
        {
            return ctors.Concat<ITypeMember>(fields).Concat(methods).Concat(properties).ToArray();
        }

        public IMethod[] GetMethods()
        {
            return methods.ToArray();
        }

        public IProperty[] GetProperties()
        {
            return properties.ToArray();
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
            return this;
        }

        public IPointerType MakePointerType(PointerKind PointerKind)
        {
            return new DescribedPointerType(this, PointerKind);
        }

        public IVectorType MakeVectorType(int[] Dimensions)
        {
            return new DescribedVectorType(this, Dimensions);
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return Template.GetAttributes();
        }

        protected IMemberNamer GetMemberNamer()
        {
            return DeclaringNamespace.GetMemberNamer();
        }

        public string Name
        {
            get { return GetMemberNamer().Name(Template); }
        }

        public IEnumerable<IType> GetGenericArguments()
        {
            return Template.GetGenericArguments();
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return Template.GetGenericParameters();
        }

        #endregion

        #region Method Overloads

        public IEnumerable<IPythonMethod> GetOverloadResolvedMethods()
        {
            Dictionary<string, PythonOverloadedMethod> overloaded = new Dictionary<string, PythonOverloadedMethod>();
            Dictionary<string, IPythonMethod> simple = new Dictionary<string, IPythonMethod>();
            foreach (var item in this.ctors.Concat(this.methods))
            {
                if (overloaded.ContainsKey(item.Name))
                {
                    overloaded[item.Name].Methods.Add(item);
                }
                else if (simple.ContainsKey(item.Name))
                {
                    var other = simple[item.Name];
                    simple.Remove(item.Name);
                    overloaded[item.Name] = new PythonOverloadedMethod(item, other);
                }
                else
                {
                    simple[item.Name] = item;
                }
            }
            return overloaded.Values.Concat(simple.Values);
        }

        #endregion

        #region ISyntaxNode Implementation

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.IndentationString = "    ";
            cb.Append("class ");
            cb.Append(Name);
            var namer = GetMemberNamer();
            var baseTypeNames = GetBaseTypes().Where((item) => !(item is PythonPrimitiveType)).Select(namer.Name).ToArray();
            if (baseTypeNames.Length > 0)
            {
                cb.Append('(');
                for (int i = 0; i < baseTypeNames.Length; i++)
                {
                    if (i > 0)
                    {
                        cb.Append(", ");
                    }
                    cb.Append(baseTypeNames[i]);
                }
                cb.Append(')');
            }
            cb.Append(':');
            cb.AppendLine();
            cb.IncreaseIndentation();
            cb.AddCodeBuilder(this.GetDocCode());
            cb.AddEmptyLine();
            bool isInterface = this.get_IsInterface();
            foreach (var item in GetOverloadResolvedMethods())
                if (!isInterface || !item.IsConstructor)
            {
                cb.AddCodeBuilder(item.GetCode());
                cb.AddEmptyLine();
            }
            foreach (var item in properties)
            {
                cb.AddCodeBuilder(item.GetCode());
            }
            cb.DecreaseIndentation();
            return cb;
        }

        #endregion

        #region ITypeBuilder Implementation

        public IFieldBuilder DeclareField(IField Template)
        {
            var field = new PythonField(this, Template);
            fields.Add(field);
            return field;
        }

        public IMethodBuilder DeclareMethod(IMethod Template)
        {
            var method = new PythonMethod(this, Template);
            if (Template.IsConstructor)
            {
                ctors.Add(method);
            }
            else
            {
                methods.Add(method);
            }
            return method;
        }

        public IPropertyBuilder DeclareProperty(IProperty Template)
        {
            PythonProperty property = new PythonProperty(this, Template);
            this.properties.Add(property);
            return property;
        }

        public IType Build()
        {
            return this;
        }

        #endregion

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            var bDepends = GetBaseTypes().OfType<IType>().Aggregate(Enumerable.Empty<ModuleDependency>(), (a, b) => a.Union(ModuleDependency.FromType(b)));
            return methods.Concat(ctors).GetDependencies().Union(bDepends);
        }
    }
}
