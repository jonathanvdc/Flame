using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public class XmlDocumentationFormatter : IDocumentationFormatter
    {
        private XmlDocumentationFormatter()
        {
            
        }

        private static XmlDocumentationFormatter inst;
        public static XmlDocumentationFormatter Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new XmlDocumentationFormatter();
                }
                return inst;
            }
        }

        public string Format(IEnumerable<DescriptionAttribute> Documentation)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in Documentation)
            {
                sb.Append('<');
                sb.Append(item.Tag.ToLower());
                foreach (var attr in item.Attributes)
                {
                    sb.Append(' ');
                    sb.Append(attr.Key);
                    sb.Append("=\"");
                    sb.Append(attr.Value);
                    sb.Append('"');
                }
                sb.Append('>');
                sb.AppendLine();
                sb.AppendLine(DocumentationExtensions.IntroducePunctuationLineBreaks(item.Description));
                sb.Append("</");
                sb.Append(item.Tag);
                sb.Append('>');
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
