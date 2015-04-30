using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public sealed class PunctuationDocumentationFormatter : IDocumentationFormatter
    {
        private PunctuationDocumentationFormatter()
        {

        }

        private static PunctuationDocumentationFormatter instance;
        public static PunctuationDocumentationFormatter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new PunctuationDocumentationFormatter();
                }
                return instance;
            }
        }

        public string Format(IEnumerable<DescriptionAttribute> Attributes)
        {
            StringBuilder sb = new StringBuilder();
            var summaryAttr = Attributes.WithTag("summary");
            if (summaryAttr != null)
            {
                sb.AppendLine(DocumentationExtensions.IntroducePunctuationLineBreaks(summaryAttr.Description));
            }
            foreach (var item in Attributes.ExcludeTag("summary"))
            {
                sb.AppendLine(DocumentationExtensions.ChangeFirstCharacter(item.Tag.ToLower(), char.ToUpper) + ":");
                sb.AppendLine(DocumentationExtensions.IntroducePunctuationLineBreaks(item.Description));
            }
            return sb.ToString();
        }
    }
}
