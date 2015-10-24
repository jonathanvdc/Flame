using Flame.Build;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRTypeBuilder : IRTypeDefinition, ITypeBuilder, INamespaceBuilder
    {
        public IRTypeBuilder(IRAssemblyBuilder Assembly, INamespace DeclaringNamespace, ITypeSignatureTemplate Template)
            : base(DeclaringNamespace, new IRSignature(Template.Name))
        {
            this.Assembly = Assembly;
            this.Template = new TypeSignatureInstance(Template, this);
        }

        public IRAssemblyBuilder Assembly { get; private set; }
        public TypeSignatureInstance Template { get; private set; }

        private void AddMember(INodeStructure<ITypeMember> Member)
        {
            this.MemberNodes = new NodeCons<ITypeMember>(Member, this.MemberNodes);
        }

        private void AddNestedType(INodeStructure<IType> NestedType)
        {
            this.NestedTypeNodes = new NodeCons<IType>(NestedType, this.NestedTypeNodes);
        }

        public IFieldBuilder DeclareField(IFieldSignatureTemplate Template)
        {
            var field = new IRFieldBuilder(Assembly, this, Template);
            AddMember(field);
            return field;
        }

        public IMethodBuilder DeclareMethod(IMethodSignatureTemplate Template)
        {
            var method = new IRMethodBuilder(Assembly, this, Template);
            AddMember(method);
            return method;
        }

        public IPropertyBuilder DeclareProperty(IPropertySignatureTemplate Template)
        {
            var property = new IRPropertyBuilder(Assembly, this, Template);
            AddMember(property);
            return property;
        }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            var prototype = new DescribedType(Name, this);
            prototype.AddAttribute(PrimitiveAttributes.Instance.StaticTypeAttribute);
            var ty = new IRTypeBuilder(Assembly, this, new TypePrototypeTemplate(prototype));
            AddNestedType(ty);
            return ty;
        }

        public ITypeBuilder DeclareType(ITypeSignatureTemplate Template)
        {
            var ty = new IRTypeBuilder(Assembly, this, Template);
            AddNestedType(ty);
            return ty;
        }

        public IType Build()
        {
            return this;
        }

        INamespace IMemberBuilder<INamespace>.Build()
        {
            return this;
        }

        public void Initialize()
        {
            this.Signature = IREmitHelpers.CreateSignature(Assembly, Template.Name, Template.Attributes.Value);
            this.BaseTypeNodes = new NodeList<IType>(
                Template.BaseTypes.Value.Select(Assembly.TypeTable.GetReferenceStructure).ToArray());
            this.GenericParameterNodes = 
                IREmitHelpers.ConvertGenericParameters(Assembly, this, Template.GenericParameters.Value);
        }
    }
}
