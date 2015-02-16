using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public abstract class StaticSystemTypeBase : IType
    {
        public INamespace DeclaringNamespace
        {
            get { return SystemNamespace.Instance; }
        }

        public abstract string Name { get; }
        public abstract IMethod[] GetMethods();

        public IContainerType AsContainerType()
        {
            return null;
        }

        public IType[] GetBaseTypes()
        {
            return new IType[0];
        }

        public IMethod[] GetConstructors()
        {
            return new IMethod[0];
        }

        public IBoundObject GetDefaultValue()
        {
            throw new InvalidOperationException();
        }

        public IField[] GetFields()
        {
            return new IField[0];
        }

        public IType GetGenericDeclaration()
        {
            return this;
        }

        public ITypeMember[] GetMembers()
        {
            return GetMethods();
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
            throw new InvalidOperationException();
        }

        public IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            return this;
        }

        public IPointerType MakePointerType(PointerKind PointerKind)
        {
            throw new InvalidOperationException();
        }

        public IVectorType MakeVectorType(int[] Dimensions)
        {
            throw new InvalidOperationException();
        }

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); }
        }

        public IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[0];
        }

        public IEnumerable<IType> GetGenericArguments()
        {
            return new IType[0];
        }

        public IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return new IGenericParameter[0];
        }
    }
}
