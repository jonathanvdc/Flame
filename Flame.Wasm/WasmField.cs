using System;
using Flame.Compiler;
using Flame.Compiler.Build;
using System.Collections.Generic;

namespace Flame.Wasm
{
	public class WasmField : IField, ILiteralField, IFieldBuilder
	{
        public WasmField(IType DeclaringType, IFieldSignatureTemplate Template, WasmModuleData ModuleData)
		{
			this.DeclaringType = DeclaringType;
			this.TemplateInstance = new FieldSignatureInstance(Template, this);
            this.ModuleData = ModuleData;
			this.Value = null;
		}

		public IType DeclaringType { get; private set; }
		public FieldSignatureInstance TemplateInstance { get; private set; }
		public IBoundObject Value { get; private set; }

        public WasmModuleData ModuleData { get; private set; }

		public string Name { get { return TemplateInstance.Name; } }
		public string FullName { get { return MemberExtensions.CombineNames(DeclaringType.FullName, Name); } }
		public bool IsStatic { get { return TemplateInstance.IsStatic; } }
		public IEnumerable<IAttribute> Attributes { get { return TemplateInstance.Attributes.Value; } }
		public IType FieldType { get { return TemplateInstance.FieldType.Value; } }

		public void SetValue(IExpression Value)
		{
			this.Value = Value.EvaluateOrNull();
		}

		public void Initialize()
		{ }

		public IField Build()
		{
			return this;
		}
	}
}

