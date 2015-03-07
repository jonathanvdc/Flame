using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Emit
{
    public class PartialArguments
    {
        public PartialArguments(params ICppBlock[] Arguments)
        {
            this.Arguments = Arguments;
        }

        public ICppBlock[] Arguments { get; private set; }

        public void AssertCount(int Count)
        {
            if (Arguments.Length != Count)
            {
                throw new ArgumentException("'Arguments' has " + Arguments.Length + " elements, " + Count + " were expected.");
            }
        }

        public ICppBlock Get(int Index)
        {
            return Arguments[Index];
        }
        public ICppBlock[] GetArguments(int Count)
        {
            AssertCount(Count);
            return Arguments;
        }
        public ICppBlock Single()
        {
            return Arguments.Single();
        }
    }
}
