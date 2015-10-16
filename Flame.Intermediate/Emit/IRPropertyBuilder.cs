using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRPropertyBuilder : IRProperty, IPropertyBuilder
    {
        public IRPropertyBuilder(IRAssemblyBuilder Assembly, IType DeclaringType, IPropertySignatureTemplate Template)
            : base(DeclaringType, new IRSignature(Template.Name), Template.IsStatic)
        {
            this.Assembly = Assembly;
            this.Template = new PropertySignatureInstance(Template, this);
        }

        public IRAssemblyBuilder Assembly { get; private set; }
        public PropertySignatureInstance Template { get; private set; }

        public IMethodBuilder DeclareAccessor(AccessorType Type, IMethodSignatureTemplate Template)
        {
            var accessor = new IRAccessorBuilder(Assembly, this, Type, Template);
            this.AccessorNodes = new NodeCons<IAccessor>(accessor, this.AccessorNodes);
            return accessor;
        }

        public IProperty Build()
        {
            return this;
        }

        public void Initialize()
        {
            this.PropertyTypeNode = Assembly.TypeTable.GetReferenceStructure(Template.PropertyType.Value);
            this.Signature = IREmitHelpers.CreateSignature(Assembly, Template.Name, Template.Attributes.Value);
            this.IndexerParameterNodes = IREmitHelpers.ConvertParameters(Assembly, Template.IndexerParameters.Value);
        }
    }
}
