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
            get { return ContractIterableType.Instance; }
        }

        public string Name
        {
            get { return "Contract"; }
        }

        public IType RootType
        {
            get { return ContractObjectType.Instance; }
        }
    }
}
