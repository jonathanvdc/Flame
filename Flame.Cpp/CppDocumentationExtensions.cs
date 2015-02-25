using Flame.Compiler;
using Flame.CodeDescription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp
{
    public static class CppDocumentationExtensions
    {
        public static CodeBuilder GetDocumentationComments(this ICppMember Member)
        {
            return Member.Environment.DocumentationBuilder.GetDocumentationComments(Member);
        }
    }
}
