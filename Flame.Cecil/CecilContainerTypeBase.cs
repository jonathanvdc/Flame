using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public abstract class CecilContainerTypeBase : CecilTypeBase, IContainerType, IEquatable<IContainerType>
    {
        public CecilContainerTypeBase(ICecilType ElementType)
            : base(ElementType.Module)
        {
            this.ElementType = ElementType;
        }
        public CecilContainerTypeBase(ICecilType ElementType, CecilModule Module)
            : base(Module)
        {
            this.ElementType = ElementType;
        }

        public ICecilType ElementType { get; private set; }

        public override bool IsContainerType
        {
            get { return true; }
        }

        public override IContainerType AsContainerType()
        {
            return this;
        }

        public abstract ContainerTypeKind ContainerKind { get; }

        public IType GetElementType()
        {
            return ElementType;
        }

        public override IType GetGenericDeclaration()
        {
            return this;
        }

        public override IEnumerable<IGenericParameter> GetGenericParameters()
        {
            return Enumerable.Empty<IGenericParameter>();
        }

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return new CustomAttribute[0];
        }

        public virtual IArrayType AsArrayType()
        {
            return this as IArrayType;
        }

        public virtual IPointerType AsPointerType()
        {
            return this as IPointerType;
        }

        public virtual IVectorType AsVectorType()
        {
            return this as IVectorType;
        }

        public override IType ResolveTypeParameter(IGenericParameter TypeParameter)
        {
            return null;
        }

        public override IEnumerable<IType> GetGenericArguments()
        {
            return new IType[0];
        }

        #region Equality

        protected abstract bool ContainerEquals(IContainerType other);

        public override bool Equals(object obj)
        {
            if (obj is IContainerType)
            {
                return Equals((IContainerType)obj);
            }
            else
            {
                return false;
            }
        }

        public override bool Equals(ICecilType other)
        {
            if (other is IContainerType)
            {
                return Equals((IContainerType)other);
            }
            else
            {
                return false;
            }
        }

        public virtual bool Equals(IContainerType other)
        {
            if (ContainerEquals(other))
            {
                return GetElementType().Equals(other.GetElementType());
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return ContainerKind.GetHashCode() ^ GetElementType().GetHashCode();
        }

        #endregion
    }
}
