using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Emit
{
    public class IRAccessorBuilder : IRAccessor, IMethodBuilder
    {
        public IRAccessorBuilder(IRAssemblyBuilder Assembly, IProperty DeclaringProperty, AccessorType Type, IMethodSignatureTemplate Template)
            : base(DeclaringProperty, new IRSignature(Template.Name), Type, Template.IsStatic)
        {
            this.Assembly = Assembly;
            this.Template = new MethodSignatureInstance(Template, this);
            this.codeGen = new IRCodeGenerator(Assembly, this);
        }

        private IRCodeGenerator codeGen;

        public IRAssemblyBuilder Assembly { get; private set; }
        public MethodSignatureInstance Template { get; private set; }

        public ICodeGenerator GetBodyGenerator()
        {
            return codeGen;
        }

        public void SetMethodBody(ICodeBlock Body)
        {
            var processedNode = NodeBlock.ToNode(codeGen.Postprocess(Body));
            this.BodyNode = new LazyNodeStructure<IStatement>(processedNode, () => { throw new InvalidOperationException("IR accessor builders cannot be decompiled."); });
        }

        public IMethod Build()
        {
            return this;
        }

        public void Initialize()
        {
            this.Signature = IREmitHelpers.CreateSignature(Assembly, Template.Name, Template.Attributes.Value);
            this.ParameterNodes = IREmitHelpers.ConvertParameters(Assembly, Template.Parameters.Value);
            this.ReturnTypeNode = Assembly.TypeTable.GetReferenceStructure(Template.ReturnType.Value);
            this.BaseMethodNodes = new NodeList<IMethod>(
                Template.BaseMethods.Value.Select(Assembly.MethodTable.GetReferenceStructure).ToArray());
        }
    }
}
