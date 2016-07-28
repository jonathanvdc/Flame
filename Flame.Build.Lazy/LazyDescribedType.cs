﻿using System;
using Flame.Build;
using System.Collections.Generic;
using System.Threading;

namespace Flame.Build.Lazy
{
    public class LazyDescribedType : LazyDescribedMember, IType, INamespace
	{
		public LazyDescribedType(
			UnqualifiedName Name, INamespace Namespace,
			Action<LazyDescribedType> AnalyzeBody)
			: base(Name)
		{
			this.DeclaringNamespace = Namespace;
            this.analyzeBody = new DeferredInitializer<LazyDescribedType>(AnalyzeBody);
			this.baseTypes = new List<IType>();
            this.methods = new List<IMethod>();
            this.fields = new List<IField>();
            this.properties = new List<IProperty>();
            this.nestedTypes = new List<IType>();
            this.typeParams = new List<IGenericParameter>();
		}

		private List<IType> baseTypes;
		private List<IMethod> methods;
		private List<IField> fields;
		private List<IProperty> properties;
		private List<IType> nestedTypes;
		private List<IGenericParameter> typeParams;

		private DeferredInitializer<LazyDescribedType> analyzeBody;

		/// <summary>
		/// Gets the declaring namespace.
		/// </summary>
		/// <value>The declaring namespace.</value>
		public INamespace DeclaringNamespace { get; private set; }

		public override QualifiedName FullName
		{
			get
			{
				return Name.Qualify(DeclaringNamespace.FullName);
			}
		}

		public IAssembly DeclaringAssembly { get { return DeclaringNamespace.DeclaringAssembly; } }

		public IAncestryRules AncestryRules { get { return DefinitionAncestryRules.Instance; } }

		public IBoundObject GetDefaultValue()
		{
			return null;
		}

		protected override void CreateBody()
		{
            analyzeBody.Initialize(this);
		}

		/// <summary>
		/// Gets this described type's base types.
		/// </summary>
		public IEnumerable<IType> BaseTypes
		{
			get
			{
				CreateBody();
				return baseTypes;
			}
		}

		/// <summary>
		/// Adds the given type to this described type's set of base types.
		/// </summary>
		public void AddBaseType(IType BaseType)
		{
			this.baseTypes.Add(BaseType);
		}

		/// <summary>
		/// Gets all methods stored in this described type.
		/// </summary>
		public IEnumerable<IMethod> Methods
		{
			get
			{
				CreateBody(); 
				return methods;
			}
		}

		/// <summary>
		/// Adds the given method to this described type's list of methods.
		/// </summary>
		public void AddMethod(IMethod Method)
		{
			this.methods.Add(Method);
		}

		/// <summary>
		/// Gets all properties stored in this described type.
		/// </summary>
		public IEnumerable<IProperty> Properties
		{
			get
			{
				CreateBody();
				return properties;
			}
		}

		/// <summary>
		/// Adds the given property to this described type's list of properties.
		/// </summary>
		public void AddProperty(IProperty Property)
		{
			this.properties.Add(Property);
		}

		/// <summary>
		/// Gets all fields stored in this described type.
		/// </summary>
		public IEnumerable<IField> Fields
		{
			get
			{
				CreateBody(); 
				return fields;
			}
		}

		/// <summary>
		/// Adds the given field to this described type's list of fields.
		/// </summary>
		public void AddField(IField Field)
		{
			this.fields.Add(Field);
		}

		/// <summary>
		/// Gets all nested types stored in this described type.
		/// </summary>
		public IEnumerable<IType> Types
		{
			get
			{
				CreateBody(); 
				return this.nestedTypes;
			}
		}

		/// <summary>
		/// Adds the given type to this described type's list of nested
		/// types.
		/// </summary>
		public void AddNestedType(IType Type)
		{
			this.nestedTypes.Add(Type);
		}

		/// <summary>
		/// Adds the given generic parameter to this described type's
		/// type parameter list.
		/// </summary>
		public void AddGenericParameter(IGenericParameter Value)
		{
			this.typeParams.Add(Value);
		}

		public IEnumerable<IGenericParameter> GenericParameters
		{
			get
			{
				CreateBody();
				return this.typeParams;
			}
		}
	}
}
