using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public sealed class BlockDocumentationFormatter : IDocumentationFormatter
    {
        public BlockDocumentationFormatter(int SuggestedBlockWidth, int MaxBlockWidth)
        {
            this.SuggestedBlockWidth = SuggestedBlockWidth;
            this.MaxBlockWidth = MaxBlockWidth;
        }

        public int SuggestedBlockWidth { get; private set; }
        public int MaxBlockWidth { get; private set; }

        public static BlockDocumentationFormatter Default
        {
            get
            {
                return new BlockDocumentationFormatter(DefaultSuggestedBlockWidth, DefaultMaxBlockWidth);
            }
        }

        public const int DefaultSuggestedBlockWidth = 30;
        public const int DefaultMaxBlockWidth = 60;

        public string Format(IEnumerable<DescriptionAttribute> Attributes)
        {
            StringBuilder sb = new StringBuilder();
            var summaryAttr = Attributes.WithTag("summary");
            if (summaryAttr != null)
            {
                sb.AppendLine(DocumentationExtensions.IntroduceBlockLineBreaks(summaryAttr.Description, SuggestedBlockWidth, MaxBlockWidth));
            }
            foreach (var item in Attributes.ExcludeTag("summary"))
            {
                sb.AppendLine(DocumentationExtensions.ChangeFirstCharacter(item.Tag.ToLower(), char.ToUpper) + ":");
                sb.AppendLine(DocumentationExtensions.IntroduceBlockLineBreaks(item.Description, SuggestedBlockWidth, MaxBlockWidth));
            }
            return sb.ToString();
        }
    }
}
