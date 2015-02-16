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

        public abstract string Name { get; }

        public virtual IAssembly DeclaringAssembly
        {
            get { return null; }
        }

        public virtual string FullName
        {
            get { return Name; }
        }

        public virtual IEnumerable<IAttribute> GetAttributes()
        {
            return Enumerable.Empty<IAttribute>();
        }

        #region Types

        public IType[] GetTypes()
        {
            return types.ToArray();
        }

        private List<IType> types;
        public void Register(IType Type)
        {
            types.Add(Type);
        }

        #endregion
    }
}
