using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.TextContract
{
    public class ContractEnvironment : IEnvironment
    {
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
