using System;
using System.Collections.Generic;
using System.Linq;
using Flame;
using Flame.Compiler.Build;
using Flame.Wasm.Emit;
using Flame.Compiler;
using Flame.Build;

namespace Flame.Wasm
{
    public class WasmMethod : IMethod, IMethodBuilder
    {
        public WasmMethod(IType DeclaringType, IMethodSignatureTemplate Template, WasmModuleData ModuleData)
        {
            this.DeclaringType = DeclaringType;
            this.TemplateInstance = new MethodSignatureInstance(Template, this);
            this.ModuleData = ModuleData;
            this.WasmName = WasmHelpers.GetWasmName(this);
        }

        public IType DeclaringType { get; private set; }
        public MethodSignatureInstance TemplateInstance { get; private set; }
        public WasmModuleData ModuleData { get; private set; }
        public CodeBlock Body { get; private set; }

        private WasmCodeGenerator bodyGen;

        /// <summary>
        /// Gets this method's actual name in the module.
        /// </summary>
        public string WasmName { get; private set; }

        public UnqualifiedName Name { get { return TemplateInstance.Name; } }
        public QualifiedName FullName { get { return Name.Qualify(DeclaringType.FullName); } }
        public bool IsStatic { get { return TemplateInstance.Template.IsStatic; } }
        public bool IsConstructor { get { return TemplateInstance.IsConstructor; } }

        public AttributeMap Attributes { get { return TemplateInstance.Attributes.Value; } }
        public IEnumerable<IMethod> BaseMethods { get { return TemplateInstance.BaseMethods.Value; } }
        public IType ReturnType { get { return TemplateInstance.ReturnType.Value; } }
        public IEnumerable<IParameter> Parameters { get { return TemplateInstance.Parameters.Value; } }
        public IEnumerable<IGenericParameter> GenericParameters { get { return TemplateInstance.GenericParameters.Value; } }

        public bool IsImport { get { return Attributes.Contains(PrimitiveAttributes.Instance.ImportAttribute.AttributeType); } }

        public WasmCodeGenerator BodyGenerator
        {
            get
            {
                if (bodyGen == null)
                {
                    bodyGen = new WasmCodeGenerator(
                        this,
                        ModuleData.Abi,
                        ModuleData.Abi.GetSignature(this));
                }
                return bodyGen;
            }
        }
        public ICodeGenerator GetBodyGenerator()
        {
            return BodyGenerator;
        }

        public void SetMethodBody(ICodeBlock Block)
        {
            this.Body = (CodeBlock)Block;
        }

        public void Initialize()
        { }

        public IMethod Build()
        {
            return this;
        }

        /// <summary>
        /// Adds this method's definition to the given WebAssembly file builder.
        /// </summary>
        /// <param name="Builder">The file builder.</param>
        public void Declare(WasmFileBuilder Builder)
        {
            Builder.DeclareMethod(this);
        }

        /// <summary>
        /// Adds this method's definition to the given WebAssembly file builder.
        /// </summary>
        /// <param name="Builder">The file builder.</param>
        public void Define(WasmFileBuilder Builder)
        {
            if (!IsImport)
            {
                Builder.DefineMethod(
                    this,
                    BodyGenerator.WrapBody(
                        Body.ToExpression(BlockContext.TopLevel, Builder)));
            }
        }
    }
}

