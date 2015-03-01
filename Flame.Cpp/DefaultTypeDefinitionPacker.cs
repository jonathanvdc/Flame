using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public class DefaultTypeDefinitionPacker : ITypeDefinitionPacker
    {
        private DefaultTypeDefinitionPacker()
        {

        }

        private static DefaultTypeDefinitionPacker instance;
        public static DefaultTypeDefinitionPacker Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DefaultTypeDefinitionPacker();
                }
                return instance;
            }
        }

        public IEnumerable<CppMemberPack> Pack(IEnumerable<ICppMember> Members)
        {
            IEnumerable<ICppMember> ctors = Members.OfType<IMethod>().Where(item => item.IsConstructor).Cast<ICppMember>();
            IEnumerable<ICppMember> fields = Members.OfType<IField>().Cast<ICppMember>();
            IEnumerable<IProperty> props = Members.OfType<IProperty>();
            IEnumerable<ICppMember> methods = Members.OfType<IMethod>().Where(item => !item.IsConstructor).Cast<ICppMember>();

            var accessorPacks = props.Select(item => new CppMemberPack(item.GetAccessors().OfType<ICppMember>()));
            var methodPacks = methods.Select(item => new CppMemberPack(item));

            return new CppMemberPack[] { new CppMemberPack(ctors) }.Concat(methodPacks).Concat(accessorPacks).With(new CppMemberPack(fields));
        }
    }
}
