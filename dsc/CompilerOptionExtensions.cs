using Flame;
using Flame.CodeDescription;
using Flame.Compiler;
using Flame.XmlDocs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc
{
    public static class CompilerOptionExtensions
    {
        /// <summary>
        /// Tries to create a documentation builder. Returns null on failure.
        /// </summary>
        /// <param name="CompilerOptions"></param>
        /// <param name="Assembly"></param>
        /// <returns></returns>
        public static IDocumentationBuilder CreateDocumentationBuilder(this ICompilerOptions CompilerOptions, IAssembly Assembly)
        {
            string docsOption = CompilerOptions.GetOption<string>("docs", "none").ToLower();
            switch (docsOption)
            {
                case "xml":
                case "true":
                    return XmlDocumentationProvider.FromAssembly(Assembly);
                case "none":
                case "false":
                case "no":
                default:
                    return null;
            }
        }
    }
}
