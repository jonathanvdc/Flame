using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRTypeBuilder : IRTypeDefinition, ITypeBuilder
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

        public IFieldBuilder DeclareField(IFieldSignatureTemplate Template)
        {
            // TODO: implement this!
            throw new NotImplementedException();
        }

        public IMethodBuilder DeclareMethod(IMethodSignatureTemplate Template)
        {
            var method = new IRMethodBuilder(Assembly, this, Template);
            AddMember(method);
            return method;
        }

        public IPropertyBuilder DeclareProperty(IPropertySignatureTemplate Template)
        {
            // TODO: implement this!
            throw new NotImplementedException();
        }

        public IType Build()
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
