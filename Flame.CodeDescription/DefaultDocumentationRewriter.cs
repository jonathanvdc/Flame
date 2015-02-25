using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public sealed class DefaultDocumentationRewriter : IDocumentationRewriter
    {
        private DefaultDocumentationRewriter()
        {

        }

        private static DefaultDocumentationRewriter inst;
        public static DefaultDocumentationRewriter Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new DefaultDocumentationRewriter();
                }
                return inst;
            }
        }
        
        private static string ProcessAccessorSummary(IAccessor Accessor, string Summary)
        {
            string trimmedDocstr = Summary.Trim();
            if (trimmedDocstr.StartsWith("gets or sets ", StringComparison.InvariantCultureIgnoreCase))
            {
                trimmedDocstr = trimmedDocstr.Substring("gets or sets ".Length);
            }
            else if (trimmedDocstr.StartsWith("gets ", StringComparison.InvariantCultureIgnoreCase))
            {
                trimmedDocstr = trimmedDocstr.Substring("gets ".Length);
            }
            else if (trimmedDocstr.StartsWith("sets ", StringComparison.InvariantCultureIgnoreCase))
            {
                trimmedDocstr = trimmedDocstr.Substring("sets ".Length);
            }
            else
            {
                return Summary;
            }
            if (Accessor.get_IsGetAccessor())
            {
                return "Gets " + DocumentationExtensions.ChangeFirstCharacter(trimmedDocstr, char.ToLower);
            }
            else if (Accessor.get_IsSetAccessor())
            {
                return "Sets " + DocumentationExtensions.ChangeFirstCharacter(trimmedDocstr, char.ToLower);
            }
            else
            {
                return Summary;
            }
        }

        private static string ProcessSummary(IMember Member, string Summary)
        {
            if (Member is IAccessor)
            {
                return ProcessAccessorSummary((IAccessor)Member, Summary);
            }
            else
            {
                return Summary;
            }
        }

        private static DescriptionAttribute ProcessAttribute(DescriptionAttribute Attribute, IMember Member)
        {
            if (Attribute.Tag.Equals("summary", StringComparison.InvariantCultureIgnoreCase))
            {
                return new DescriptionAttribute(Attribute.Tag, ProcessSummary(Member, Attribute.Description));
            }
            else
            {
                return Attribute;
            }
        }

        public IEnumerable<DescriptionAttribute> Rewrite(IEnumerable<DescriptionAttribute> Attributes, IMember Member)
        {
            return Attributes.Select((item) => ProcessAttribute(item, Member));
        }
    }
}
