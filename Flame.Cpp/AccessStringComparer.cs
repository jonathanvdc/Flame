using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class AccessStringComparer : IComparer<string>
    {
        private static int Transform(string x)
        {
            switch (x.ToLower())
            {
                case "public":
                    return 2;
                case "protected":
                    return 1;
                case "private":
                    return 0;
                default:
                    return -1;
            }
        }

        public int Compare(string x, string y)
        {
            return Transform(x).CompareTo(Transform(y));
        }
    }
}
