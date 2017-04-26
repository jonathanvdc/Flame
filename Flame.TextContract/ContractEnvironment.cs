using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public class ContractEnvironment : IEnvironment
    {
        private ContractEnvironment() { }

        static ContractEnvironment()
        {
            Instance = new ContractEnvironment();
        }

        public static ContractEnvironment Instance { get; private set; }

        public IType EnumerableType
        {
            get { return ContractIterableType.Instance; }
        }

        public IType EnumeratorType
        {
            get { return ContractIteratorType.Instance; }
        }

        public string Name
        {
            get { return "Contract"; }
        }

        public IType RootType
        {
            get { return ContractObjectType.Instance; }
        }

        public IEnumerable<IType> GetDefaultBaseTypes(
            IType Type, IEnumerable<IType> DefaultBaseTypes)
        {
            if (Type.GetIsInterface())
                return Enumerable.Empty<IType>();

            foreach (var baseTy in DefaultBaseTypes)
            {
                if (!baseTy.GetIsInterface())
                    return Enumerable.Empty<IType>();
            }
            return new IType[] { RootType };
        }

        /// <inheritdoc/>
        public IType GetEquivalentType(IType Type)
        {
            return Type;
        }
    }
}
