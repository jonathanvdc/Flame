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

                var attrs = item.Attributes;

                foreach (var attr in attrs.Keys)
                {
                    var val = attrs.Get<string>(attr, "");

                    if (!string.IsNullOrEmpty(val))
                    {
                        sb.Append(' ');
                        sb.Append(attr);
                        sb.Append("=\"");
                        sb.Append(val);
                        sb.Append('"');
                    }
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
