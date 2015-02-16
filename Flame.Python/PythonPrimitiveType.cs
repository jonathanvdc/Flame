using Flame.Build;
using Flame.Compiler.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public abstract class PythonPrimitiveType : IType
    {
        protected PythonPrimitiveType()
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
            var paramlessCtor = new DescribedMethod("__init__", this);
            paramlessCtor.ReturnType = PrimitiveTypes.Void;
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

        public virtual IField[] GetFields()
        {
            return new IField[0];
        }

        public IType GetGenericDeclaration()
        {
            return this;
        }

        public virtual ITypeMember[] GetMembers()
        {
            return GetConstructors().Concat<ITypeMember>(GetMethods()).Concat(GetProperties()).Concat(GetFields()).ToArray();
        }

        public virtual IMethod[] GetMethods()
        {
            return new IMethod[0];
        }

        public virtual IProperty[] GetProperties()
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

        public IType MakeGenericType(IEnumerable<IType> TypeArguments)
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

        public string FullName
        {
            get { return Name; }
        }

        public virtual IEnumerable<IAttribute> GetAttributes()
        {
            //return new IAttribute[] { PrimitiveAttributes.RootTypeAttribute };
            return new IAttribute[0];
        }

        public abstract string Name { get; }

        public IEnumerable<IType> GetGenericArguments()
        {
            return new IType[0];
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return new IGenericParameter[0];
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
