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
        public CecilContainerTypeBase(IType ElementType, CecilModule Module)
            : base(Module)
        {
            this.ElementType = ElementType;
        }

        public IType ElementType { get; private set; }

        public override IEnumerable<IGenericParameter> GenericParameters
        {
            get { return Enumerable.Empty<IGenericParameter>(); }
        }

        protected override IList<CustomAttribute> GetCustomAttributes()
        {
            return new CustomAttribute[0];
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
                return ElementType.Equals(other.ElementType);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode() ^ ElementType.GetHashCode();
        }

        #endregion

        IType IContainerType.ElementType
        {
            get { return ElementType; }
        }
    }
}
