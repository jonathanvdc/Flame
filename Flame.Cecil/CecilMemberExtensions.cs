using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public static class CecilMemberExtensions
    {
        public static ModuleDefinition GetModule(this ICecilMember CecilMember)
        {
            /*if (CecilMember is IMethod)
            {
                var method = (IMethod)CecilMember;
                if (method.get_IsGenericInstance())
                {
                    return ((ICecilMember)method.GetGenericDeclaration()).GetModule();
                }
            }*/
            return CecilMember.GetMemberReference().Module;
        }

        public static IEnumerable<T> Prefer<T>(this IEnumerable<T> Sequence, IEnumerable<T> Preferred)
        {
            var pEnum = Preferred.GetEnumerator();
            var oEnum = Sequence.GetEnumerator();
            try
            {
                bool more = true;

                while (pEnum.MoveNext())
                {
                    if (more)
                        more = oEnum.MoveNext();
                    yield return pEnum.Current;
                }

                if (more)
                    while (oEnum.MoveNext())
                    {
                        yield return oEnum.Current;
                    }
            }
            finally
            {
                pEnum.Dispose();
                oEnum.Dispose();
            }
        }
    }
}
