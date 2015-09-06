using Flame.Build;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public abstract class ContractPrimitiveType : IType
    {
        protected ContractPrimitiveType()
        {
        }

        public IContainerType AsContainerType()
        {
            return null;
        }

        public INamespace DeclaringNamespace
        {
            get
            {
                return null;
            }
        }

        public IEnumerable<IType> BaseTypes
        {
            get { return new IType[0]; }
        }

        public IBoundObject GetDefaultValue()
        {
            return NullExpression.Instance;
        }

        public IEnumerable<IField> Fields
        {
            get { return new IField[0]; }
        }

        public IEnumerable<IMethod> Methods
        {
            get
            {
                var paramlessCtor = new DescribedMethod("Create" + char.ToUpper(Name[0]).ToString() + Name.Substring(1), this);
                paramlessCtor.IsConstructor = true;
                return new IMethod[]
                {
                    paramlessCtor
                };
            }
        }

        public IEnumerable<IProperty> Properties
        {
            get { return new IProperty[0]; }
        }

        public virtual string FullName
        {
            get { return Name; }
        }

        public virtual IEnumerable<IAttribute> Attributes
        {
            get { return new IAttribute[0]; }
        }

        public abstract string Name { get; }

        public virtual IEnumerable<IGenericParameter> GenericParameters
        {
            get { return new IGenericParameter[0]; }
        }

        public override string ToString()
        {
            return Name;
        }

        public IAncestryRules AncestryRules
        {
            get { return DefinitionAncestryRules.Instance; }
        }
    }
}
