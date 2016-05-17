using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Build;
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
        public PythonClass(ITypeSignatureTemplate Template, INamespace DeclaringNamespace)
        {
            this.Template = new TypeSignatureInstance(Template, this);
            this.DeclaringNamespace = DeclaringNamespace;
            this.fields = new List<IField>();
            this.methods = new List<IPythonMethod>();
            this.properties = new List<PythonProperty>();
        }

        public TypeSignatureInstance Template { get; private set; }
        public INamespace DeclaringNamespace { get; private set; }

        #region IType Implementation

        public IContainerType AsContainerType()
        {
            return null;
        }

        public IEnumerable<IType> BaseTypes
        {
            get { return Template.BaseTypes.Value; }
        }

        private List<IField> fields;
        private List<IPythonMethod> methods;
        private List<PythonProperty> properties;

        public IBoundObject GetDefaultValue()
        {
            return NullExpression.Instance;
        }

        public IEnumerable<IField> Fields
        {
            get { return fields; }
        }

        public IEnumerable<IMethod> Methods
        {
            get { return methods; }
        }

        public IEnumerable<IProperty> Properties
        {
            get { return properties; }
        }

        public QualifiedName FullName
        {
            get { return Name.Qualify(DeclaringNamespace.FullName); }
        }

        public AttributeMap Attributes
        {
            get { return Template.Attributes.Value; }
        }

        protected IMemberNamer GetMemberNamer()
        {
            return DeclaringNamespace.GetMemberNamer();
        }

        public UnqualifiedName Name
        {
            get
            {
                var descTy = new DescribedType(Template.Name, DeclaringNamespace);
                return new SimpleName(GetMemberNamer().Name(descTy)); 
            }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Template.GenericParameters.Value; }
        }

        #endregion

        #region Method Overloads

        public IEnumerable<IPythonMethod> GetOverloadResolvedMethods()
        {
            var overloaded = new Dictionary<UnqualifiedName, PythonOverloadedMethod>();
            var simple = new Dictionary<UnqualifiedName, IPythonMethod>();
            foreach (var item in this.methods)
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
            cb.Append(GenericNameExtensions.TrimGenerics(Name.ToString()));
            var namer = GetMemberNamer();
            var baseTypeNames = BaseTypes.Where((item) => !(item is PythonPrimitiveType)).Select(namer.Name).ToArray();
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
            bool isInterface = this.GetIsInterface();
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

        public IFieldBuilder DeclareField(IFieldSignatureTemplate Template)
        {
            var field = new PythonField(this, Template);
            fields.Add(field);
            return field;
        }

        public IMethodBuilder DeclareMethod(IMethodSignatureTemplate Template)
        {
            var method = new PythonMethod(this, Template);
            methods.Add(method);
            return method;
        }

        public IPropertyBuilder DeclareProperty(IPropertySignatureTemplate Template)
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
            var bDepends = BaseTypes.OfType<IType>().Aggregate(Enumerable.Empty<ModuleDependency>(), (a, b) => a.Union(ModuleDependency.FromType(b)));
            return methods.GetDependencies().Union(bDepends);
        }

        public IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
        }

        public void Initialize()
        {
            // Do nothing. This back-end does not need `Initialize` to get things done.
        }
    }
}
