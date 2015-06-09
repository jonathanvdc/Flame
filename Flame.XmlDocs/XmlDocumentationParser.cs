using Flame.Compiler;
using Flame.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Pixie;

namespace Flame.XmlDocs
{
    public class XmlDocumentationParser : IDocumentationParser
    {
        private XmlDocumentationParser()
        {

        }

        static XmlDocumentationParser()
        {
            Instance = new XmlDocumentationParser();
        }

        public static XmlDocumentationParser Instance { get; private set; }

        public IEnumerable<IAttribute> Parse(string Documentation, SourceLocation Location, ICompilerLog Log)
        {
            if (string.IsNullOrWhiteSpace(Documentation))
            {
                return new IAttribute[] { };
            }

            string docPlusRoot = "<docs>" + Documentation + "</docs>";
            var textReader = new StringReader(docPlusRoot);
            var xmlReader = XmlReader.Create(textReader);
            var attrs = new List<IAttribute>();
            try
            {
                var doc = new XmlDocument();
                doc.Load(xmlReader);
                return MarkupNodeSerializer.ToMarkupNodes(doc.DocumentElement.ChildNodes)
                    .Select(item => item.get_IsTextNode() ? new DescriptionAttribute("summary", item) : new DescriptionAttribute(item)).ToArray();
            }
            catch (XmlException ex)
            {
                Log.LogWarning(new LogEntry(
                    "Invalid XML documentation",
                    ex.Message,
                    Location));
            }

            return attrs;
        }
    }
}
