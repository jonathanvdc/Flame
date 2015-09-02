using Flame.Build;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class CecilAncestryRules : IAncestryRules
    {
        private CecilAncestryRules()
        { }

        static CecilAncestryRules()
        {
            Instance = new CecilAncestryRules();
        }

        public static CecilAncestryRules Instance { get; private set; }

        private bool CheckIfEquivalentPrimitive(IType First, IType Second)
        {
            if (First.get_IsPrimitive())
            {
                return First.Equals(Second);
            }
            else
            {
                return First.FullName == Second.FullName;
            }
        }

        public int GetAncestryDegree(IType First, IType Second)
        {
            if (First.get_IsPrimitive())
            {
                if (CheckIfEquivalentPrimitive(Second, First))
                {
                    return 0;
                }
            }
            else if (Second.get_IsPrimitive())
            {
                if (CheckIfEquivalentPrimitive(First, Second))
                {
                    return 0;
                }
            }
            return DefinitionAncestryRules.Instance.GetAncestryDegree(First, Second);
        }
    }
}
