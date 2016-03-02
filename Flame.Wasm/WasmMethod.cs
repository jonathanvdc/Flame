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
		public WasmExpr Body { get; private set; }

		private WasmCodeGenerator bodyGen;

		/// <summary>
		/// Gets this method's actual name in the module.
		/// </summary>
		public string WasmName { get; private set; }

		public string Name { get { return TemplateInstance.Name; } }
		public string FullName { get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); } }
		public bool IsStatic { get { return TemplateInstance.Template.IsStatic; } }
		public bool IsConstructor { get { return TemplateInstance.IsConstructor; } }

		public IEnumerable<IAttribute> Attributes { get { return TemplateInstance.Attributes.Value; } }
		public IEnumerable<IMethod> BaseMethods { get { return TemplateInstance.BaseMethods.Value; } }
		public IType ReturnType { get { return TemplateInstance.ReturnType.Value; } }
		public IEnumerable<IParameter> Parameters { get { return TemplateInstance.Parameters.Value; } }
		public IEnumerable<IGenericParameter> GenericParameters { get { return TemplateInstance.GenericParameters.Value; } }

        public bool IsImport { get { return Attributes.HasAttribute(PrimitiveAttributes.Instance.ImportAttribute.AttributeType); } }

		public WasmCodeGenerator BodyGenerator 
		{
			get 
			{ 			
				if (bodyGen == null)
                    bodyGen = new WasmCodeGenerator(this, ModuleData.Abi);
				return bodyGen;
			} 
		}
		public ICodeGenerator GetBodyGenerator()
		{
			return BodyGenerator;
		}

		public void SetMethodBody(ICodeBlock Block)
		{
			this.Body = CodeBlock.ToExpression(Block);
		}

		public void Initialize()
		{ }

		public IMethod Build()
		{
			return this;
		}

		public CodeBuilder ToCode()
		{
            var cb = new CodeBuilder();
            var args = new List<WasmExpr>();
            args.Add(new IdentifierExpr(WasmName));
            args.AddRange(ModuleData.Abi.GetSignature(this));
            if (IsImport)
            {
                var importAbi = ModuleData.Abi.ImportAbi;

                var importFunc = new DescribedMethod("__import_" + Name, DeclaringType, ReturnType, IsStatic);
                foreach (var item in Parameters)
                {
                    var descParam = new DescribedParameter(
                        null, item.ParameterType);
                    importFunc.AddParameter(descParam);
                }

                var importArgs = new List<WasmExpr>();
                importArgs.Add(new IdentifierExpr(WasmHelpers.GetWasmName(importFunc)));
                importArgs.Add(new StringExpr(DeclaringType.Name));
                importArgs.Add(new StringExpr(Name));
                importArgs.AddRange(importAbi.GetSignature(importFunc));
                cb.AddCodeBuilder(new CallExpr(OpCodes.DeclareImport, importArgs).ToCode());

                var argLayout = ModuleData.Abi.GetArgumentLayout(this);
                // Synthesize a method body that performs a call_import
                var importCall = importAbi.CreateDirectCall(
                                     importFunc, argLayout.ThisPointer.CreateGetExpression(), 
                                     Parameters.Select((item, i) => 
                                        argLayout.GetArgument(i).CreateGetExpression())
                                 .ToArray());
                args.Add(CodeBlock.ToExpression(
                    BodyGenerator.EmitReturn(importCall.Emit(BodyGenerator))));
            }
            else
            {
                args.AddRange(BodyGenerator.WrapBody(Body));
            }
            cb.AddCodeBuilder(new CallExpr(OpCodes.DeclareFunction, args).ToCode());
            return cb;
		}

		public override string ToString()
		{
			return ToCode().ToString();
		}
	}
}

