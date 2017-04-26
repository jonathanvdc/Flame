using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS
{
    public class MarsEnvironment : IEnvironment
    {
        private MarsEnvironment()
        { }

        static MarsEnvironment()
        {
            Instance = new MarsEnvironment();
        }

        public static MarsEnvironment Instance { get; private set; }

        public IType EnumerableType
        {
            get { return null; }
        }

        public IType EnumeratorType
        {
            get { return null; }
        }

        public string Name
        {
            get { return "MIPS/MARS"; }
        }

        public IType RootType
        {
            get { return null; }
        }

        public IEnumerable<IType> GetDefaultBaseTypes(
            IType Type, IEnumerable<IType> DefaultBaseTypes)
        {
            return Enumerable.Empty<IType>();
        }

        /// <inheritdoc/>
        public IType GetEquivalentType(IType Type)
        {
            return Type;
        }
    }
}
