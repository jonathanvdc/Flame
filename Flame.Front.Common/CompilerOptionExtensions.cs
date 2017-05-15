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
        /// <param name="TargetAssembly">
        /// The target assembly, which is the result of linking the documented assemblies.
        /// Its signature used to generate documentation, but its contents are not.</param>
        /// <param name="SourceAssemblies">
        /// The assemblies which are linked into the target assembly. Their contents (but not
        /// their signatures) are used to generate documentation.
        /// </param>
        /// <returns></returns>
        public static IDocumentationBuilder CreateDocumentationBuilder(
            this ICompilerOptions CompilerOptions,
            IAssembly TargetAssembly,
            IEnumerable<IAssembly> SourceAssemblies)
        {
            string docsOption = (CompilerOptions.GetOption<string>("docs", "none") ?? "").ToLower();
            switch (docsOption)
            {
                case "xml":
                case "true":
                case "":
                    return XmlDocumentationProvider.FromAssemblies(TargetAssembly, SourceAssemblies);
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
