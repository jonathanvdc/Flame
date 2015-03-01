using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    /// <summary>
    /// A type that represents a number of packed C++ members.
    /// </summary>
    public class CppMemberPack
    {
        public CppMemberPack(params ICppMember[] Members)
        {
            this.Members = Members;
        }
        public CppMemberPack(IEnumerable<ICppMember> Members)
        {
            this.Members = Members;
        }

        public IEnumerable<ICppMember> Members { get; private set; }

        public bool IsEmpty
        {
            get { return !Members.Any(); }
        }

        public CodeBuilder GetHeaderCode()
        {
            CodeBuilder cb = new CodeBuilder();
            foreach (var item in Members)
            {
                cb.AddCodeBuilder(item.GetHeaderCode());
            }
            return cb;
        }
    }

    /// <summary>
    /// Provides an interface for objects that pack a class or struct definition's contents. 
    /// </summary>
    public interface ITypeDefinitionPacker
    {
        IEnumerable<CppMemberPack> Pack(IEnumerable<ICppMember> Members);
    }
}
