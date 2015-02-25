using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public sealed class DefaultDocumentationFormatter : IDocumentationFormatter
    {
        private DefaultDocumentationFormatter()
        {

        }

        private static DefaultDocumentationFormatter instance;
        public static DefaultDocumentationFormatter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DefaultDocumentationFormatter();
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
                sb.AppendLine(DocumentationExtensions.IntroduceLineBreaks(summaryAttr.Description));
            }
            foreach (var item in Attributes.ExcludeTag("summary"))
            {
                sb.AppendLine(DocumentationExtensions.ChangeFirstCharacter(item.Tag.ToLower(), char.ToUpper) + ":");
                sb.AppendLine(DocumentationExtensions.IntroduceLineBreaks(item.Description));
            }
            return sb.ToString();
        }
    }
}
