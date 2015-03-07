using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public sealed class VerbatimDocumentationFormatter : IDocumentationFormatter
    {
        private VerbatimDocumentationFormatter()
        {

        }

        private static VerbatimDocumentationFormatter instance;
        public static VerbatimDocumentationFormatter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new VerbatimDocumentationFormatter();
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
                sb.AppendLine(summaryAttr.Description);
            }
            foreach (var item in Attributes.ExcludeTag("summary"))
            {
                sb.AppendLine(DocumentationExtensions.ChangeFirstCharacter(item.Tag.ToLower(), char.ToUpper) + ":");
                sb.AppendLine(item.Description);
            }
            return sb.ToString();
        }
    }
}
