using System;
using Flame;
using Flame.Compiler.Build;
using System.Collections.Generic;
using Flame.Wasm.Emit;
using Flame.Compiler;

namespace Flame.Wasm
{
	public class WasmMethod : IMethod, IMethodBuilder
	{
		public WasmMethod(IType DeclaringType, IMethodSignatureTemplate Template, IWasmAbi Abi)
		{
			this.DeclaringType = DeclaringType;
			this.TemplateInstance = new MethodSignatureInstance(Template, this);
			this.Abi = Abi;
			this.WasmName = WasmHelpers.GetWasmName(this);
		}

		public IType DeclaringType { get; private set; }
		public MethodSignatureInstance TemplateInstance { get; private set; }
		public IWasmAbi Abi { get; private set; }
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

		public WasmCodeGenerator BodyGenerator 
		{
			get 
			{ 			
				if (bodyGen == null)
					bodyGen = new WasmCodeGenerator(this, Abi);
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
			var args = new List<WasmExpr>();
			args.Add(new IdentifierExpr(WasmName));
			args.AddRange(Abi.GetSignature(this));
			args.AddRange(BodyGenerator.WrapBody(Body));
			return new CallExpr(OpCodes.DeclareFunction, args).ToCode();
		}

		public override string ToString()
		{
			return ToCode().ToString();
		}
	}
}

