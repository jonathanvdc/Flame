using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    /// <summary>
    /// A documentation formatter for Doxygen.
    /// </summary>
    public class DoxygenFormatter : IDocumentationFormatter
    {
        public DoxygenFormatter()
        {
            this.TagMap = DefaultTagMap;
            this.ArgumentMap = DefaultArgumentMap;
            this.VerbatimTags = DefaultVerbatimTags;
        }
        public DoxygenFormatter(IReadOnlyDictionary<string, string> TagMap)
        {
            this.TagMap = TagMap;
            this.ArgumentMap = DefaultArgumentMap;
            this.VerbatimTags = DefaultVerbatimTags;
        }
        public DoxygenFormatter(IReadOnlyDictionary<string, string> TagMap, IReadOnlyDictionary<string, Func<DescriptionAttribute, string>> ArgumentMap)
        {
            this.TagMap = TagMap;
            this.ArgumentMap = ArgumentMap;
            this.VerbatimTags = DefaultVerbatimTags;
        }
        public DoxygenFormatter(IReadOnlyDictionary<string, string> TagMap, 
            IReadOnlyDictionary<string, Func<DescriptionAttribute, string>> ArgumentMap,
            ISet<string> VerbatimTags)
        {
            this.TagMap = TagMap;
            this.ArgumentMap = ArgumentMap;
            this.VerbatimTags = VerbatimTags;
        }

        #region Defaults

        private static IReadOnlyDictionary<string, string> defaultTagMapVal;
        public static IReadOnlyDictionary<string, string> DefaultTagMap
        {
            get
            {
                if (defaultTagMapVal == null)
                {
                    defaultTagMapVal = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                    { 
                        { "summary", "brief" },
                        { "returns", "return" },
                        { "param", "param" },
                        { "remarks", "remark" },
                        { "pre", "pre" },
                        { "in", "pre" },
                        { "precond", "pre" },
                        { "requires", "pre" },
                        { "post", "post" },
                        { "out", "post" },
                        { "postcond", "post" },
                        { "ensures", "post" }
                    };
                }
                return defaultTagMapVal;
            }
        }

        private static IReadOnlyDictionary<string, Func<DescriptionAttribute, string>> defaultArgumentMapVal;
        public static IReadOnlyDictionary<string, Func<DescriptionAttribute, string>> DefaultArgumentMap
        {
            get
            {
                if (defaultArgumentMapVal == null)
                {
                    defaultArgumentMapVal = new Dictionary<string, Func<DescriptionAttribute, string>>(StringComparer.InvariantCultureIgnoreCase)
                    {
                        { "param", (attr) => attr.Attributes.ContainsKey("name") ? attr.Attributes["name"] : "" }
                    };
                }
                return defaultArgumentMapVal;
            }
        }

        private static ISet<string> defaultVerbatimTagsVal;
        public static ISet<string> DefaultVerbatimTags
        {
            get
            {
                if (defaultVerbatimTagsVal == null)
                {
                    defaultVerbatimTagsVal = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase)
                    {
                        "pre",
                        "post",
                        "example"
                    };
                }
                return defaultVerbatimTagsVal;
            }
        }

        #endregion

        public IReadOnlyDictionary<string, string> TagMap { get; private set; }
        public IReadOnlyDictionary<string, Func<DescriptionAttribute, string>> ArgumentMap { get; private set; }
        public ISet<string> VerbatimTags { get; private set; }

        public string GetDoxygenTag(string Tag)
        {
            if (TagMap.ContainsKey(Tag))
            {
                return TagMap[Tag];
            }
            else
            {
                return Tag.ToLower();
            }
        }

        public string GetDoxygenArguments(DescriptionAttribute Attribute)
        {
            if (ArgumentMap.ContainsKey(Attribute.Tag))
            {
                return ArgumentMap[Attribute.Tag](Attribute);
            }
            else
            {
                return "";
            }
        }

        public string Format(IEnumerable<DescriptionAttribute> Documentation)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in Documentation)
            {
                sb.Append('\\');
                string doxyTag = GetDoxygenTag(item.Tag);
                sb.Append(doxyTag);
                string args = GetDoxygenArguments(item);
                if (!string.IsNullOrWhiteSpace(args))
                {
                    sb.Append(' ');
                    sb.Append(args.Trim());
                }
                sb.Append(' ');
                if (VerbatimTags.Contains(doxyTag))
                {
                    sb.AppendLine(item.Description.TrimStart());
                }
                else
                {
                    sb.AppendLine(DocumentationExtensions.IntroduceLineBreaks(item.Description).TrimStart());
                }
            }
            return sb.ToString();
        }
    }
}
