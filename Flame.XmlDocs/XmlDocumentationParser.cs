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
using System.Text.RegularExpressions;

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

        private const string DocWrapperName = "__docs";
        private static Dictionary<Regex, Func<Match, string>> messageRewriteRules;

        private static void InitMessageRewriteRules()
        {
            if (messageRewriteRules != null)
            {
                return;
            }

            messageRewriteRules = new Dictionary<Regex, Func<Match, string>>()
            {
                // Try to extract unterminated tag errors, which are quite common.
                {
                    new Regex("The '(?<starttag>.*?)' start tag on line [0-9]* position [0-9]* does not match the end tag of '(?<endtag>.*?)'. Line [0-9]*, position [0-9]*."),
                    match =>
                    {
                        if (match.Groups["endtag"].Value == DocWrapperName)
                        {
                            return "Start tag '" + match.Groups["starttag"].Value + "' does not have a closing tag.";
                        }
                        else
                        {
                            return "Start tag '" + match.Groups["starttag"].Value + "' did not match closing tag '" + match.Groups["endtag"].Value + "'.";
                        }
                    }
                }
            };
        }

        public IEnumerable<IAttribute> Parse(string Documentation, SourceLocation Location, ICompilerLog Log)
        {
            if (string.IsNullOrWhiteSpace(Documentation))
            {
                return new IAttribute[] { };
            }

            string docPlusRoot = "<" + DocWrapperName + ">" + Documentation + "</" + DocWrapperName + ">";
            var textReader = new StringReader(docPlusRoot);
            var xmlReader = XmlReader.Create(textReader);
            var attrs = new List<IAttribute>();
            try
            {
                var doc = new XmlDocument();
                doc.Load(xmlReader);
                return Pixie.Xml.XmlNodeHandler.Instance.ToMarkupNodes(doc.DocumentElement.ChildNodes)
                    .Select(item => item.GetIsTextNode() ? new DescriptionAttribute("summary", item) : new DescriptionAttribute(item)).ToArray();
            }
            catch (XmlException ex)
            {
                Log.LogWarning(new LogEntry(
                    "invalid XML documentation",
                    ExtractXmlMessage(ex.Message),
                    Location));
            }

            return attrs;
        }

        private static string ExtractXmlMessage(string Message)
        {
            InitMessageRewriteRules();

            foreach (var item in messageRewriteRules)
            {
                var match = item.Key.Match(Message);
                if (match.Success)
                {
                    return item.Value(match);
                }
            }

            return Message;
        }
    }
}
