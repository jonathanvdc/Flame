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
            IEnumerable<ICppMember> methods = Members.OfType<IMethod>().Where(item => !item.IsConstructor && !item.get_IsOperator() && !item.get_IsCast()).Cast<ICppMember>();
            IEnumerable<ICppMember> operators = Members.OfType<IMethod>().Where(item => !item.IsConstructor && item.get_IsOperator() && !item.get_IsCast()).Cast<ICppMember>();
            IEnumerable<ICppMember> casts = Members.OfType<IMethod>().Where(item => !item.IsConstructor && !item.get_IsOperator() && item.get_IsCast()).Cast<ICppMember>();
            
            Func<ICppMember, CppMemberPack> packSingle = item => new CppMemberPack(item);
            Func<IEnumerable<ICppMember>, IEnumerable<CppMemberPack>> packIndividually = items => items.Select(packSingle);

            var accessorPacks = props.Select(item => new CppMemberPack(item.GetAccessors().OfType<ICppMember>()));
            var methodPacks = packIndividually(methods);
            var opPacks = packIndividually(operators.Concat(casts));
            var typePacks = packIndividually(Members.OfType<IType>().Cast<ICppMember>());

            return new CppMemberPack[] { new CppMemberPack(ctors) }
                .Concat(typePacks)
                .Concat(methodPacks)
                .Concat(accessorPacks)
                .Concat(opPacks)
                .With(new CppMemberPack(fields));
        }
    }
}
