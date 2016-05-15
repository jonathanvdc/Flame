using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public abstract class PrimitiveNamespace : INamespace
    {
        protected PrimitiveNamespace()
        {
            this.types = new List<IType>();
        }

        public abstract UnqualifiedName Name { get; }

        public virtual IAssembly DeclaringAssembly
        {
            get { return null; }
        }

        public virtual QualifiedName FullName
        {
            get { return new QualifiedName(Name); }
        }

        public virtual AttributeMap Attributes
        {
            get { return AttributeMap.Empty; }
        }

        #region Types

        public IEnumerable<IType> Types
        {
            get { return types; }
        }

        private List<IType> types;
        public void Register(IType Type)
        {
            types.Add(Type);
        }

        #endregion
    }
}
