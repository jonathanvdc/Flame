using System;
using Flame.Build;
using System.Collections.Generic;
using System.Threading;

namespace Flame.Build.Lazy
{
    /// <summary>
    /// A type implementation that constructs itself lazily.
    /// </summary>
    public class LazyDescribedType : LazyDescribedMember, IType, INamespace
	{
        /// <summary>
        /// Creates a new lazily described type from the given name,
        /// declaring namespace, and a deferred construction action.
        /// </summary>
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
		/// Gets the declaring namespace for this type.
		/// </summary>
		/// <value>The declaring namespace.</value>
		public INamespace DeclaringNamespace { get; private set; }

        /// <summary>
        /// Gets this type's qualified name.
        /// </summary>
		public override QualifiedName FullName
		{
			get
			{
				return Name.Qualify(DeclaringNamespace.FullName);
			}
		}

        /// <summary>
        /// Gets this type's declaring assembly.
        /// </summary>
		public IAssembly DeclaringAssembly
        {
            get { return DeclaringNamespace.DeclaringAssembly; }
        }

        /// <summary>
        /// Gets this type's set of ancestry rules.
        /// </summary>
		public IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
        }

        /// <summary>
        /// Gets this lazily described type's default value.
        /// </summary>
		public IBoundObject GetDefaultValue()
		{
			return null;
		}

        /// <summary>
        /// Constructs the initial state of this lazily described member.
        /// This method is called on-demand.
        /// </summary>
		protected override void CreateBody()
		{
            analyzeBody.Initialize(this);
		}

		/// <summary>
		/// Gets this described type's set of base types.
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
            CreateBody();
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
            CreateBody();
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
            CreateBody();
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
            CreateBody();
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
            CreateBody();
			this.nestedTypes.Add(Type);
		}

		/// <summary>
		/// Adds the given generic parameter to this described type's
		/// type parameter list.
		/// </summary>
		public void AddGenericParameter(IGenericParameter Value)
		{
            CreateBody();
			this.typeParams.Add(Value);
		}

        /// <summary>
        /// Gets this type's list of generic parameters.
        /// </summary>
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
