﻿using System;
using System.Collections.Generic;
using System.Linq;
using Flame;
using Flame.Build;
using Flame.Compiler.Build;

namespace Flame.Wasm
{
	public class WasmType : IType, ITypeBuilder
	{
		public WasmType(
			INamespace DeclaringNamespace, ITypeSignatureTemplate Template)
		{
			this.DeclaringNamespace = DeclaringNamespace;
			this.TemplateInstance = new TypeSignatureInstance(Template, this);
			this.methodList = new List<WasmMethod>();
		}

		public INamespace DeclaringNamespace { get; private set; }
		public TypeSignatureInstance TemplateInstance { get; private set; }

		public string Name { get { return TemplateInstance.Name; } }
		public string FullName { get { return MemberExtensions.CombineNames(DeclaringNamespace.Name, Name); } }

		public IEnumerable<IAttribute> Attributes { get { return TemplateInstance.Attributes.Value; } }
		public IEnumerable<IType> BaseTypes { get { return TemplateInstance.BaseTypes.Value; } }
		public IEnumerable<IGenericParameter> GenericParameters { get { return TemplateInstance.GenericParameters.Value; } }

		public IAncestryRules AncestryRules
		{
			get { return DefinitionAncestryRules.Instance; }
		}

		private List<WasmMethod> methodList;

		public IEnumerable<IField> Fields
		{
			get { return Enumerable.Empty<IField>(); }
		}

		public IEnumerable<IMethod> Methods
		{
			get { return methodList; }
		}

		public IEnumerable<IProperty> Properties
		{
			get { return Enumerable.Empty<IProperty>(); }
		}

		public IBoundObject GetDefaultValue()
		{
			return null;
		}

		public IFieldBuilder DeclareField(IFieldSignatureTemplate Template)
		{
			throw new NotImplementedException();
		}

		public IMethodBuilder DeclareMethod(IMethodSignatureTemplate Template)
		{
			var result = new WasmMethod(this, Template);
			methodList.Add(result);
			return result;
		}

		public IPropertyBuilder DeclareProperty(IPropertySignatureTemplate Template)
		{
			throw new NotImplementedException();
		}

		public void Initialize()
		{ }

		public IType Build()
		{
			return this;
		}
	}
}

