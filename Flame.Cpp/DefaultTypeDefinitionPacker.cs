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

        private IEnumerable<CppMemberPack> PackIndividually(IEnumerable<ICppMember> Items)
        {
            return Items.Select(item => new CppMemberPack(item));
        }
        
        private IEnumerable<CppMemberPack> PackSortedMethods(IEnumerable<ICppMember> Members)
        {
            return PackIndividually(Members.Cast<IMethod>().OrderBy(item => item, DefaultMethodComparer.Instance).Cast<ICppMember>());
        }

        public IEnumerable<CppMemberPack> Pack(IEnumerable<ICppMember> Members)
        {
            IEnumerable<ICppMember> ctors = Members.OfType<IMethod>().Where(item => item.IsConstructor).OrderBy(item => item.GetParameters().Length).Cast<ICppMember>();
            IEnumerable<ICppMember> fields = Members.OfType<IField>().OrderBy(item => item.Name).Cast<ICppMember>();
            IEnumerable<IProperty> props = Members.OfType<IProperty>().OrderBy(item => item.Name);
            IEnumerable<ICppMember> methods = Members.OfType<IMethod>().Where(item => !item.IsConstructor && !item.GetIsOperator() && !item.GetIsCast()).Cast<ICppMember>();
            IEnumerable<ICppMember> operators = Members.OfType<IMethod>().Where(item => !item.IsConstructor && (item.GetIsOperator() || item.GetIsCast())).Cast<ICppMember>();

            var accessorPacks = props.Select(item => new CppMemberPack(item.Accessors.OfType<ICppMember>()));
            var methodPacks = PackSortedMethods(methods);
            var opPacks = PackSortedMethods(operators);
            var typePacks = PackIndividually(Members.OfType<IType>().Cast<ICppMember>());

            return new CppMemberPack[] { new CppMemberPack(ctors) }
                .Concat(typePacks)
                .Concat(methodPacks)
                .Concat(accessorPacks)
                .Concat(opPacks)
                .With(new CppMemberPack(fields));
        }
    }
}
