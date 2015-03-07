using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public class SelectingDocumentationFormatter : IDocumentationFormatter
    {
        public SelectingDocumentationFormatter(Func<DescriptionAttribute, IDocumentationFormatter> GetFormatter)
        {
            this.GetFormatter = GetFormatter;
        }

        public Func<DescriptionAttribute, IDocumentationFormatter> GetFormatter { get; private set; }

        public string Format(IEnumerable<DescriptionAttribute> Documentation)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in Documentation)
            {
                sb.AppendLine(GetFormatter(item).Format(new DescriptionAttribute[] { item }).TrimEnd());
            }
            return sb.ToString();
        }
    }
}
