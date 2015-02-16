using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public abstract class PrimitiveBase : IType
    {
        public PrimitiveBase()
        {

        }

        #region Abstract

        public abstract string Name { get; }
        public abstract IEnumerable<IAttribute> GetAttributes();
        public abstract INamespace DeclaringNamespace { get; }
        public abstract IMethod[] GetConstructors();
        public abstract IField[] GetFields();
        public abstract IMethod[] GetMethods();
        public abstract IProperty[] GetProperties();

        #endregion

        #region General

        public string FullName
        {
            get { return MemberExtensions.CombineNames(DeclaringNamespace.FullName, Name); }
        }

        public virtual IType[] GetBaseTypes()
        {
            return new IType[0];
        }

        public virtual IBoundObject GetDefaultValue()
        {
            return null;
        }

        #endregion

        #region Members

        public virtual ITypeMember[] GetMembers()
        {
            return GetMethods().Concat<ITypeMember>(GetConstructors()).Concat(GetProperties()).Concat(GetFields()).ToArray();
        }

        #endregion

        #region Container Types

        public virtual IContainerType AsContainerType()
        {
            return this as IContainerType;
        }
        public virtual bool IsContainerType
        {
            get { return this is IContainerType; }
        }

        public virtual IArrayType MakeArrayType(int Rank)
        {
            return new DescribedArrayType(this, Rank);
        }

        public virtual IPointerType MakePointerType(PointerKind PointerKind)
        {
            return new DescribedPointerType(this, PointerKind);
        }

        public virtual IVectorType MakeVectorType(int[] Dimensions)
        {
            return new DescribedVectorType(this, Dimensions);
        }

        #endregion

        #region Generics

        public virtual IType GetGenericDeclaration()
        {
            return this;
        }

        public virtual IType MakeGenericType(IEnumerable<IType> TypeArguments)
        {
            if (!TypeArguments.Any())
            {
                return this;
            }
            else
            {
                return new DescribedGenericTypeInstance(this, TypeArguments);
            }
        }

        public virtual IEnumerable<IType> GetGenericArguments()
        {
            return Enumerable.Empty<IType>();
        }

        public virtual IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return Enumerable.Empty<IGenericParameter>();
        }

        #endregion
    }
}
