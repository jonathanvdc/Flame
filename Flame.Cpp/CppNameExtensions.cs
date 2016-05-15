using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public static class CppNameExtensions
    {
        public static string[] SplitScope(QualifiedName Name)
        {
            var results = new List<string>();
            for (var n = Name; !n.IsEmpty; n = Name.Name)
            {
                results.Add(n.Qualifier.ToString());
            }
            return results.ToArray();
        }

        public static string[] SplitScope(string Name)
        {
            return Name.Split(new string[] { "::", "." }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static string JoinScope(string[] NameParts)
        {
            return string.Join("::", NameParts);
        }

        public static string[] RemoveRedundantScope(string[] Name, string[] CurrentNamespace)
        {
            int i = 0;
            while (i < Name.Length - 1 && i < CurrentNamespace.Length && Name[i] == CurrentNamespace[i])
            {
                i++;
            }
            string[] subArr = new string[Name.Length - i];
            Array.Copy(Name, i, subArr, 0, subArr.Length);
            return subArr;
        }

        public static string RemoveRedundantScope(string Name, string CurrentNamespace)
        {
            return JoinScope(RemoveRedundantScope(SplitScope(Name), SplitScope(CurrentNamespace)));
        }

        public static string RemoveRedundantScope(string Name, INamespace CurrentNamespace)
        {
            return RemoveRedundantScope(Name, CurrentNamespace.FullName.ToString());
        }

        public static string Name(this Func<INamespace, IConverter<IType, string>> TypeNamer, IType Value, INamespace CurrentNamespace)
        {
            return TypeNamer(CurrentNamespace).Convert(Value);
        }

        public static INamespace GetEnclosingNamespace(this IType Type)
        {
            if (Type.DeclaringNamespace is IType)
            {
                return ((IType)Type.DeclaringNamespace).GetEnclosingNamespace();
            }
            else
            {
                return Type.DeclaringNamespace;
            }
        }

        public static INamespace GetEnclosingNamespace(this ITypeMember Member)
        {
            return Member.DeclaringType.GetEnclosingNamespace();
        }

        public static string Name(this Func<INamespace, IConverter<IType, string>> TypeNamer, IType Value, IType CurrentType)
        {
            return Name(TypeNamer, Value, GetEnclosingNamespace(CurrentType));
        }

        public static string Name(this Func<INamespace, IConverter<IType, string>> TypeNamer, IType Value, ITypeMember CurrentMember)
        {
            return Name(TypeNamer, Value, GetEnclosingNamespace(CurrentMember));
        }

        public static string Name(this Func<INamespace, IConverter<IType, string>> TypeNamer, IType Value, ICodeGenerator CurrentCodeGenerator)
        {
            return Name(TypeNamer, Value, CurrentCodeGenerator.Method);
        }
    }
}
