using System;
using Flame.Build;
using System.Collections.Generic;
using Flame.Compiler;
using System.Threading;

namespace Flame.Build.Lazy
{
    public abstract class LazyDescribedMember : IMember
    {
        public LazyDescribedMember(UnqualifiedName Name)
        {
            this.Name = Name;
            this.attributeList = new AttributeMapBuilder();
        }

        public UnqualifiedName Name { get; private set; }
        public abstract QualifiedName FullName { get; }

        protected abstract void CreateBody();

        private AttributeMapBuilder attributeList;

        public void AddAttribute(IAttribute Attribute)
        {
            CreateBody();
            attributeList.Add(Attribute);
        }

        public void AddAttributes(IEnumerable<IAttribute> Attributes)
        {
            CreateBody();
            attributeList.AddRange(Attributes);
        }

        public void AddAttributes(AttributeMap Attributes)
        {
            CreateBody();
            attributeList.AddRange(Attributes);
        }

        public void AddAttributes(AttributeMapBuilder Attributes)
        {
            CreateBody();
            attributeList.AddRange(Attributes);
        }

        public AttributeMap Attributes
        {
            get 
            { 
                CreateBody();
                return new AttributeMap(attributeList);
            }
        }
    }

    public abstract class LazyDescribedTypeMember : LazyDescribedMember, ITypeMember
	{
		public LazyDescribedTypeMember(UnqualifiedName Name, IType DeclaringType)
			: base(Name)
		{
			this.DeclaringType = DeclaringType;
		}

		/// <summary>
		/// Gets the type that declared this member.
		/// </summary>
		/// <value>The type of the declaring.</value>
		public IType DeclaringType { get; private set; }

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

		public abstract bool IsStatic { get; set; }

		public override string ToString()
		{
            return FullName.ToString();
		}
	}

	public class LazyDescribedMethod : LazyDescribedTypeMember, IBodyMethod
	{
		public LazyDescribedMethod(UnqualifiedName Name, IType DeclaringType, Action<LazyDescribedMethod> AnalyzeBody)
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

		private bool isStaticVal;

		public override bool IsStatic
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

		private bool isCtorVal;

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
		/// <returns></returns>
		public IStatement GetMethodBody()
		{
			CreateBody();
			return Body;
		}

		protected override void CreateBody()
		{
            analyzeBody.Initialize(this);
		}

		private List<IParameter> parameters;

		public virtual void AddParameter(IParameter Parameter)
		{
			parameters.Add(Parameter);
		}

		public IEnumerable<IParameter> Parameters
		{
			get
			{
				CreateBody();
				return parameters;
			}
		}

		private List<IMethod> baseMethods;

		public virtual void AddBaseMethod(IMethod Method)
		{
			baseMethods.Add(Method);
		}

		public IEnumerable<IMethod> BaseMethods
		{
			get
			{
				CreateBody();
				return baseMethods.ToArray();
			}
		}

		private List<IGenericParameter> genericParams;

		public virtual void AddGenericParameter(IGenericParameter Parameter)
		{
			genericParams.Add(Parameter);
		}

		/// <summary>
		/// Gets this method's generic parameters.
		/// </summary>
		/// <returns></returns>
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

