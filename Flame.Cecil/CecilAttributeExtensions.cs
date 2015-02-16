using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public static class CecilAttributeExtensions
    {
        public static string GetDefaultMember(this TypeDefinition Definition)
        {
            foreach (var item in Definition.CustomAttributes)
            {
                if (item.AttributeType.FullName == typeof(System.Reflection.DefaultMemberAttribute).FullName && item.ConstructorArguments.Count == 1)
                {
                    return item.ConstructorArguments[0].Value.ToString();
                }
            }
            return null;
        }

        public static void SetDefaultMember(this TypeDefinition Definition, string MemberName)
        {
            var defaultMemberType = Definition.Module.Import(typeof(System.Reflection.DefaultMemberAttribute));
            var arg = new CustomAttributeArgument(Definition.Module.TypeSystem.String, MemberName);
            foreach (var item in Definition.CustomAttributes)
            {
                if (item.AttributeType.FullName == defaultMemberType.FullName && item.ConstructorArguments.Count == 1)
                {
                    item.ConstructorArguments[0] = arg; // Overwrite the constructor argument
                    return;
                }
            }
            var resolvedDefaultMember = defaultMemberType.Resolve();
            var ctor = resolvedDefaultMember.Methods.First((item) => item.IsConstructor && item.Parameters.Count == 1 && item.Parameters[0].ParameterType == resolvedDefaultMember.Module.TypeSystem.String);
            var attr = new CustomAttribute(Definition.Module.Import(ctor));
            attr.ConstructorArguments.Add(arg);
            Definition.CustomAttributes.Add(attr);
        }
    }
}
