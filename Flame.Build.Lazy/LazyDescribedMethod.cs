using System;
using Flame.Build;
using System.Collections.Generic;
using Flame.Compiler;
using System.Threading;

namespace Flame.Build.Lazy
{
    /// <summary>
    /// A base class for members that are constructed lazily.
    /// </summary>
    public abstract class LazyDescribedMember : IMember
    {
        /// <summary>
        /// Initializes this lazily described member with the given name.
        /// </summary>
        public LazyDescribedMember(UnqualifiedName Name)
        {
            this.Name = Name;
            this.attributeList = new AttributeMapBuilder();
        }

        /// <summary>
        /// Gets this member's unqualified name.
        /// </summary>
        public UnqualifiedName Name { get; private set; }

        /// <summary>
        /// Gets this member's qualified name.
        /// </summary>
        public abstract QualifiedName FullName { get; }

        /// <summary>
        /// Constructs the initial state of this lazily described member.
        /// This method is called on-demand.
        /// </summary>
        protected abstract void CreateBody();

        private AttributeMapBuilder attributeList;

        /// <summary>
        /// Adds the given attribute to this member's attribute map.
        /// </summary>
        public void AddAttribute(IAttribute Attribute)
        {
            CreateBody();
            attributeList.Add(Attribute);
        }

        /// <summary>
        /// Adds the given sequence of attributes to this member's attributes map.
        /// </summary>
        public void AddAttributes(IEnumerable<IAttribute> Attributes)
        {
            CreateBody();
            attributeList.AddRange(Attributes);
        }

        /// <summary>
        /// Adds the given attribute map to this member's attributes map.
        /// </summary>
        public void AddAttributes(AttributeMap Attributes)
        {
            CreateBody();
            attributeList.AddRange(Attributes);
        }

        /// <summary>
        /// Adds the given attribute map builder to this member's attributes map.
        /// </summary>
        public void AddAttributes(AttributeMapBuilder Attributes)
        {
            CreateBody();
            attributeList.AddRange(Attributes);
        }

        /// <summary>
        /// Gets this member's attribute map.
        /// </summary>
        public AttributeMap Attributes
        {
            get
            {
                CreateBody();
                return new AttributeMap(attributeList);
            }
        }

        /// <summary>
        /// Creates a string that identifies this member.
        /// </summary>
        public override string ToString()
        {
            return FullName.ToString();
        }
    }

    /// <summary>
    /// A base class for type members that are constructed lazily.
    /// </summary>
    public abstract class LazyDescribedTypeMember : LazyDescribedMember, ITypeMember
	{
        /// <summary>
        /// Initializes this lazily described member with the given name and
        /// declaring type.
        /// </summary>
		public LazyDescribedTypeMember(UnqualifiedName Name, IType DeclaringType)
			: base(Name)
		{
			this.DeclaringType = DeclaringType;
		}

		/// <summary>
		/// Gets the type that declared this member.
		/// </summary>
		/// <value>The type that declared this member.</value>
		public IType DeclaringType { get; private set; }

        /// <summary>
        /// Gets this member's qualified name.
        /// </summary>
		public sealed override QualifiedName FullName
		{
			get
			{
				if (DeclaringType == null)
                    return Name.Qualify();
                else
                    return Name.Qualify(DeclaringType.FullName);
			}
		}

        private bool isStaticVal;

        /// <summary>
        /// Gets or sets a boolean flag that indicates whether this type member
        /// is static or not.
        /// </summary>
        public bool IsStatic
        {
            get
            {
                CreateBody();
                return isStaticVal;
            }
            set
            {
                CreateBody();
                isStaticVal = value;
            }
        }
	}

    /// <summary>
    /// A method implementation that constructs itself lazily.
    /// </summary>
	public class LazyDescribedMethod : LazyDescribedTypeMember, IBodyMethod
	{
        /// <summary>
        /// Creates a new lazily described method from the given name,
        /// declaring type, and a deferred construction action.
        /// </summary>
		public LazyDescribedMethod(
            UnqualifiedName Name, IType DeclaringType,
            Action<LazyDescribedMethod> AnalyzeBody)
			: base(Name, DeclaringType)
		{
			this.baseMethods = new List<IMethod>();
            this.parameters = new List<IParameter>();
            this.baseMethods = new List<IMethod>();
            this.genericParams = new List<IGenericParameter>();
            this.analyzeBody = new DeferredInitializer<LazyDescribedMethod>(AnalyzeBody);
		}

		private DeferredInitializer<LazyDescribedMethod> analyzeBody;

		private IType retType;

        /// <summary>
        /// Gets or sets this method's return type.
        /// </summary>
		public IType ReturnType
		{
			get
			{
				CreateBody();
				return retType;
			}
			set
			{
				CreateBody();
				retType = value;
			}
		}

		private bool isCtorVal;

        /// <summary>
        /// Gets or sets a boolean flag that tells if this method is a constructor.
        /// </summary>
		public bool IsConstructor
		{
			get
			{
				CreateBody();
				return isCtorVal;
			}
			set
			{
				CreateBody();
				isCtorVal = value;
			}
		}

		private IStatement bodyStmt;

        /// <summary>
        /// Gets or sets this method's body statement.
        /// </summary>
		public IStatement Body
		{
			get
			{
				CreateBody();
				return bodyStmt;
			}
			set
			{
				CreateBody();
				bodyStmt = value;
			}
		}

		/// <summary>
		/// Gets the method's body statement.
		/// </summary>
		public IStatement GetMethodBody()
		{
			CreateBody();
			return Body;
		}

        /// <summary>
        /// Constructs the initial state of this lazily described member.
        /// This method is called on-demand.
        /// </summary>
		protected override void CreateBody()
		{
            analyzeBody.Initialize(this);
		}

		private List<IParameter> parameters;

        /// <summary>
        /// Adds the given parameter to this method's parameter list.
        /// </summary>
		public virtual void AddParameter(IParameter Parameter)
		{
            CreateBody();
			parameters.Add(Parameter);
		}

        /// <summary>
        /// Gets this method's parameter list.
        /// </summary>
		public IEnumerable<IParameter> Parameters
		{
			get
			{
				CreateBody();
				return parameters;
			}
		}

		private List<IMethod> baseMethods;

        /// <summary>
        /// Makes the given method a base method of this method.
        /// </summary>
		public virtual void AddBaseMethod(IMethod Method)
		{
            CreateBody();
			baseMethods.Add(Method);
		}

        /// <summary>
        /// Gets the list of all base methods for this method.
        /// </summary>
		public IEnumerable<IMethod> BaseMethods
		{
			get
			{
				CreateBody();
				return baseMethods.ToArray();
			}
		}

		private List<IGenericParameter> genericParams;

        /// <summary>
        /// Adds the given generic parameter to this method's generic parameter
        /// list.
        /// </summary>
		public virtual void AddGenericParameter(IGenericParameter Parameter)
		{
            CreateBody();
			genericParams.Add(Parameter);
		}

		/// <summary>
		/// Gets this method's list of generic parameters.
		/// </summary>
		public IEnumerable<IGenericParameter> GenericParameters
		{
			get
			{
				CreateBody();
				return genericParams;
			}
		}
	}
}
