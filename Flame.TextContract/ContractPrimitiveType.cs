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

        public IType[] GetBaseTypes()
        {
            return new IType[0];
        }

        public IMethod[] GetConstructors()
        {
            var paramlessCtor = new DescribedMethod("Create" + char.ToUpper(Name[0]).ToString() + Name.Substring(1), this);
            paramlessCtor.IsConstructor = true;
            return new IMethod[]
            {
                paramlessCtor
            };
        }

        public IBoundObject GetDefaultValue()
        {
            return new NullExpression();
        }

        public IField[] GetFields()
        {
            return new IField[0];
        }

        public virtual IType GetGenericDeclaration()
        {
            return this;
        }

        public ITypeMember[] GetMembers()
        {
            return GetConstructors();
        }

        public IMethod[] GetMethods()
        {
            return new IMethod[0];
        }

        public IProperty[] GetProperties()
        {
            return new IProperty[0];
        }

        public bool IsContainerType
        {
            get { return false; }
        }

        public IArrayType MakeArrayType(int Rank)
        {
            return new DescribedArrayType(this, Rank);
        }

        public virtual IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            return this;
        }

        public IPointerType MakePointerType(PointerKind PointerKind)
        {
            return new DescribedPointerType(this, PointerKind);
        }

        public IVectorType MakeVectorType(int[] Dimensions)
        {
            return new DescribedVectorType(this, Dimensions);
        }

        public virtual string FullName
        {
            get { return Name; }
        }

        public virtual IEnumerable<IAttribute> GetAttributes()
        {
            //return new IAttribute[] { PrimitiveAttributes.RootTypeAttribute };
            return new IAttribute[0];
        }

        public abstract string Name { get; }

        public virtual IEnumerable<IType> GetGenericArguments()
        {
            return new IType[0];
        }

        public virtual IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return new IGenericParameter[0];
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
