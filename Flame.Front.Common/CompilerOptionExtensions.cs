using Flame;
using Flame.CodeDescription;
using Flame.Compiler;
using Flame.Front.Options;
using Flame.Recompilation;
using Flame.XmlDocs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    public static class CompilerOptionExtensions
    {
        /// <summary>
        /// Tries to create a documentation builder. Returns null on failure.
        /// </summary>
        /// <param name="CompilerOptions"></param>
        /// <param name="Assembly"></param>
        /// <returns></returns>
        public static IDocumentationBuilder CreateDocumentationBuilder(this ICompilerOptions CompilerOptions, IAssembly MainAssembly, IEnumerable<IAssembly> AuxiliaryAssemblies)
        {
            string docsOption = (CompilerOptions.GetOption<string>("docs", "none") ?? "").ToLower();
            switch (docsOption)
            {
                case "xml":
                case "true":
                case "":
                    return XmlDocumentationProvider.FromAssemblies(MainAssembly, AuxiliaryAssemblies);
                case "none":
                case "false":
                case "no":
                default:
                    return null;
            }
        }

        public static IOptionParser<string> CreateOptionParser()
        {
            var options = StringOptionParser.CreateDefault();
            options.RegisterParser<Version>(Version.Parse);
            options.RegisterParser<Flame.CodeDescription.IDocumentationFormatter>(item =>
            {
                switch (item.ToLower())
                {
                    case "doxygen":
                        return new Flame.CodeDescription.DoxygenFormatter();
                    case "xml":
                        return Flame.CodeDescription.XmlDocumentationFormatter.Instance;
                    default:
                        return Flame.CodeDescription.PunctuationDocumentationFormatter.Instance;
                }
            });
            options.RegisterParser<Flame.Syntax.IDocumentationParser>(item =>
            {
                switch (item.ToLower())
                {
                    case "xml":
                        return Flame.XmlDocs.XmlDocumentationParser.Instance;

                    case "markdown":
                        return Flame.Markdown.MarkdownDocumentationParser.Instance;

                    case "none":
                    default:
                        return Flame.Syntax.EmptyDocumentationParser.Instance;
                }
            });
            return options;
        }
    }
}
