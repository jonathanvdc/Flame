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
		public WasmMethod(IType DeclaringType, IMethodSignatureTemplate Template)
		{
			this.DeclaringType = DeclaringType;
			this.TemplateInstance = new MethodSignatureInstance(Template, this);
			this.WasmName = WasmHelpers.MangleName(this);
		}

		public IType DeclaringType { get; private set; }
		public MethodSignatureInstance TemplateInstance { get; private set; }
		public WasmCodeGenerator BodyGenerator { get; private set; }
		public WasmExpr Body { get; private set; }

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

		public ICodeGenerator GetBodyGenerator()
		{
			if (BodyGenerator == null)
				BodyGenerator = new WasmCodeGenerator(this);
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
	}
}

