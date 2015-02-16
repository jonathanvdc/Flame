using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public interface IMemberNamer
    {
        /// <summary>
        /// Names a member.
        /// </summary>
        /// <param name="Member"></param>
        /// <returns></returns>
        string Name(IMember Member);
    }
    public interface IMemberNamingAssembly : IAssembly
    {
        IMemberNamer MemberNamer { get; }
    }
    public static class MemberNamerExtensions
    {
        public static IMemberNamer GetMemberNamer(this IAssembly Assembly)
        {
            if (Assembly is IMemberNamingAssembly)
            {
                return ((IMemberNamingAssembly)Assembly).MemberNamer;
            }
            else
            {
                return new DefaultPythonMemberNamer();
            }
        }
        public static IMemberNamer GetMemberNamer(this INamespace Namespace)
        {
            if (Namespace != null)
            {
                return Namespace.DeclaringAssembly.GetMemberNamer();
            }
            else
            {
                return new DefaultPythonMemberNamer();
            }
        }
    }
}
