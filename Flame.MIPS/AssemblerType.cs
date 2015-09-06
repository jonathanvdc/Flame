using Flame.Build;
using Flame.Compiler;
using Flame.MIPS.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public class AssemblerType : ITypeBuilder, IAssemblerType
    {
        public AssemblerType(INamespace DeclaringNamespace, IType Template, IAssemblerState GlobalState)
        {
            this.DeclaringNamespace = DeclaringNamespace;
            this.Template = Template;
            this.GlobalState = GlobalState;

            this.methods = new List<AssemblerMethod>();
            this.instanceFields = new List<AssemblerField>();
            this.staticFields = new List<AssemblerField>();
        }

        public INamespace DeclaringNamespace { get; private set; }
        public IType Template { get; private set; }
        public IAssemblerState GlobalState { get; private set; }

        public IEnumerable<IType> BaseTypes
        {
            get { return Template.BaseTypes; }
        }

        #region Members

        private List<AssemblerMethod> methods;
        private List<AssemblerField> instanceFields;
        private List<AssemblerField> staticFields;

        public IEnumerable<IField> Fields
        {
            get { return instanceFields.Concat(staticFields); }
        }

        public IEnumerable<IMethod> Methods
        {
            get { return methods; }
        }

        public IEnumerable<IProperty> Properties
        {
            get { return new IProperty[0]; }
        }

        private static int GetSize(List<AssemblerField> Target)
        {
            int size = 0;
            if (Target.Count > 0)
            {
                size = Target[Target.Count - 1].Offset + Target[Target.Count - 1].Size;
            }
            return size;
        }

        private AssemblerField DeclareField(IField Template, List<AssemblerField> Target)
        {
            var field = new AssemblerField(this, Template, GetSize(Target));
            Target.Add(field);
            return field;
        }

        public IFieldBuilder DeclareField(IField Template)
        {
            if (Template.IsStatic)
            {
                return DeclareField(Template, staticFields);
            }
            else
            {
                return DeclareField(Template, instanceFields);
            }
        }

        public IMethodBuilder DeclareMethod(IMethod Template)
        {
            var method = new AssemblerMethod(this, Template, GlobalState);
            methods.Add(method);
            return method;
        }

        public IPropertyBuilder DeclareProperty(IProperty Template)
        {
            throw new NotImplementedException();
        }

        #region Size

        public int InstanceSize
        {
            get { return GetSize(instanceFields); }
        }

        public int StaticSize
        {
            get { return GetSize(staticFields); }
        }

        #endregion

        #endregion

        public IBoundObject GetDefaultValue()
        {
            if (this.get_IsReferenceType())
            {
                return Flame.Compiler.Expressions.NullExpression.Instance;
            }
            throw new NotImplementedException();
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get
            {
                if (Template.get_IsSingleton())
                {
                    return Template.Attributes.Where((item) => !(item is SingletonAttribute)).Concat(new IAttribute[] { PrimitiveAttributes.Instance.StaticTypeAttribute });
                }
                else
                {
                    return Template.Attributes;
                }
            }
        }

        public string Name
        {
            get { return Template.Name; }
        }

        public IEnumerable<IGenericParameter> GenericParameters
        {
            get { return new IGenericParameter[0]; }
        }

        public IType Build()
        {
            return this;
        }

        public IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
        }
    }
}
