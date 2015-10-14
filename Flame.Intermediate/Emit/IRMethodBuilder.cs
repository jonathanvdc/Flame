using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRMethodBuilder : IRMethod, IMethodBuilder
    {
        public IRMethodBuilder(IRAssemblyBuilder Assembly, IType DeclaringType, IMethodSignatureTemplate Template)
            : base(DeclaringType, new IRSignature(Template.Name), Template.IsStatic, Template.IsConstructor)
        {
            this.Assembly = Assembly;
            this.Template = new MethodSignatureInstance(Template, this);
        }

        public IRAssemblyBuilder Assembly { get; private set; }
        public MethodSignatureInstance Template { get; private set; }

        public ICodeGenerator GetBodyGenerator()
        {
            throw new NotImplementedException();
        }

        public void SetMethodBody(ICodeBlock Body)
        {
            throw new NotImplementedException();
        }

        public IMethod Build()
        {
            return this;
        }

        public void Initialize()
        {
            this.Signature = IREmitHelpers.CreateSignature(Assembly, Template.Name, Template.Attributes.Value);
            this.GenericParameterNodes = new NodeList<IGenericParameter>(
                Template.GenericParameters.Value.Select(item => IREmitHelpers.ConvertGenericParameter(Assembly, item)).ToArray());
            this.ParameterNodes = new NodeList<IParameter>(
                Template.Parameters.Value.Select(item => IREmitHelpers.ConvertParameter(Assembly, item)).ToArray());
            this.BaseMethodNodes = new NodeList<IMethod>(
                Template.BaseMethods.Value.Select(Assembly.MethodTable.GetReferenceStructure).ToArray());
        }
    }
}
