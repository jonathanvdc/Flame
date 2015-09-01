using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public static class DescriptionAttributeExtensions
    {
        public static bool HasTag(this DescriptionAttribute Attribute, string Tag)
        {
            return Attribute.Tag.Equals(Tag, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsSummary(this DescriptionAttribute Attribute)
        {
            return Attribute.HasTag("summary");
        }

        public static DescriptionAttribute WithTag(this IEnumerable<DescriptionAttribute> Attributes, string Tag)
        {
            return Attributes.FirstOrDefault((item) => item.HasTag(Tag));
        }

        public static bool ContainsTag(this IEnumerable<DescriptionAttribute> Attributes, string Tag)
        {
            return Attributes.Any((attr) => attr.HasTag(Tag));
        }

        public static IEnumerable<DescriptionAttribute> Merge(this IEnumerable<DescriptionAttribute> Attributes, IEnumerable<DescriptionAttribute> Other)
        {
            return Attributes.Concat(Other.Where((item) => !Attributes.ContainsTag(item.Tag)));
        }

        public static IEnumerable<DescriptionAttribute> ExcludeTag(this IEnumerable<DescriptionAttribute> Attributes, string Tag)
        {
            return Attributes.Where((item) => !item.HasTag(Tag));
        }

        public static IEnumerable<DescriptionAttribute> ExcludeTags(this IEnumerable<DescriptionAttribute> Attributes, IEnumerable<string> Tags)
        {
            IEnumerable<DescriptionAttribute> result = Attributes;
            foreach (var item in Tags)
            {
                result = result.ExcludeTag(item);
            }
            return result;
        }

        public static IEnumerable<DescriptionAttribute> ExcludeTags(this IEnumerable<DescriptionAttribute> Attributes, params string[] Tags)
        {
            return Attributes.ExcludeTags(Tags);
        }

        public static IEnumerable<DescriptionAttribute> GetDescriptionAttributes(this IMember Member)
        {
            var attrs = Member.Attributes.OfType<DescriptionAttribute>();
            if (Member is IMethod)
            {
                var baseMethods = ((IMethod)Member).BaseMethods;
                foreach (var item in baseMethods)
                {
                    attrs = attrs.Merge(item.GetDescriptionAttributes().ExcludeTag("remarks"));
                }
            }
            return attrs;
        }
    }
}