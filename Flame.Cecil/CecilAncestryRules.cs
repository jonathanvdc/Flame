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
            if (First.GetIsPrimitive())
            {
                return First.Equals(Second);
            }
            else
            {
                return First.FullName.Equals(Second.FullName);
            }
        }

        public int GetAncestryDegree(IType First, IType Second)
        {
            if (First.GetIsPrimitive())
            {
                if (CheckIfEquivalentPrimitive(Second, First))
                {
                    return 0;
                }
            }
            else if (Second.GetIsPrimitive())
            {
                if (CheckIfEquivalentPrimitive(First, Second))
                {
                    return 0;
                }
            }

            var leftMethod = CecilDelegateType.GetDelegateMethod(First);
            var rightMethod = CecilDelegateType.GetDelegateMethod(Second);

            if (leftMethod != null && rightMethod != null)
            {
                return MethodTypeAncestryRules.Instance.GetAncestryDegree(leftMethod, rightMethod);
            }

            return DefinitionAncestryRules.Instance.GetAncestryDegree(First, Second);
        }
    }
}
