using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public static class DocumentationExtensions
    {
        public static string ChangeFirstCharacter(string Value, Func<char, char> Change)
        {
            if (!string.IsNullOrEmpty(Value))
            {
                return Change(Value[0]) + Value.Substring(1);
            }
            else
            {
                return Value;
            }
        }

        private static char[] punctuation = new[] { '?', '!', '.' };

        public static string IntroduceLineBreaks(string Text)
        {
            StringBuilder sb = new StringBuilder(Text.Replace(Environment.NewLine, ""));
            for (int i = 0; i < sb.Length; i++)
            {
                if (punctuation.Contains(sb[i]))
                {
                    i++;
                    while (i + 1 < sb.Length && char.IsWhiteSpace(sb[i]))
                    {
                        i++;
                    }
                    if (i < sb.Length && char.IsUpper(sb[i]))
                    {
                        // Bingo. Insert newline.
                        sb.Insert(i, Environment.NewLine);
                    }
                }
            }
            return sb.ToString();
        }

        public static string ToDocumentation(this IEnumerable<DescriptionAttribute> Attributes, IMember Member)
        {
            return DefaultDocumentationFormatter.Instance.Format(DefaultDocumentationRewriter.Instance.Rewrite(Attributes, Member));
        }

        public static string ToXmlDocumentation(this IEnumerable<DescriptionAttribute> Attributes)
        {
            return XmlDocumentationFormatter.Instance.Format(Attributes);
        }

        public static string GetDocumentation(this IMember Member)
        {
            return Member.GetDescriptionAttributes().ToDocumentation(Member);
        }

        public static string GetDocumentation(this IMember Member, Func<DescriptionAttribute, bool> Predicate)
        {
            return Member.GetDescriptionAttributes().Where(Predicate).ToDocumentation(Member);
        }

        public static string GetXmlDocumentation(this IMember Member)
        {
            return Member.GetDescriptionAttributes().ToXmlDocumentation();
        }

        public static string GetXmlDocumentation(this IMember Member, Func<DescriptionAttribute, bool> Predicate)
        {
            return Member.GetDescriptionAttributes().Where(Predicate).ToXmlDocumentation();
        }

        public static CodeBuilder ToLineComments(string Documentation, string LineCommentPrefix)
        {
            if (string.IsNullOrWhiteSpace(Documentation))
            {
                return new CodeBuilder();
            }
            string[] doclines = Documentation.Replace(Environment.NewLine, "\n").Split('\n');
            CodeBuilder cb = new CodeBuilder();
            foreach (var item in doclines)
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    cb.AddLine(LineCommentPrefix + " " + item.Trim());
                }
            }
            return cb;
        }

        public static CodeBuilder ToBlockComments(string Documentation, string LeadingDelimiter, string TrailingDelimiter)
        {
            return ToBlockComments(Documentation, LeadingDelimiter, TrailingDelimiter, string.Empty);
        }
        public static CodeBuilder ToBlockComments(string Documentation, string LeadingDelimiter, string TrailingDelimiter, string LinePrefix)
        {
            if (string.IsNullOrWhiteSpace(Documentation))
            {
                return new CodeBuilder();
            }
            string[] doclines = Documentation.Replace(Environment.NewLine, "\n").Split('\n');
            CodeBuilder cb = new CodeBuilder();
            cb.Append(LeadingDelimiter + " ");
            string fullPrefix = string.IsNullOrWhiteSpace(LinePrefix) ? new string(' ', LeadingDelimiter.Length + 1) : new string(' ', Math.Max(LeadingDelimiter.Length - LinePrefix.Length, 0)) + LinePrefix + " ";
            cb.Append(doclines[0].Trim());
            foreach (var item in doclines.Skip(1))
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    cb.AddLine(fullPrefix + item.Trim());
                }
            }
            cb.Append(" " + TrailingDelimiter);
            return cb;
        }

        public static CodeBuilder GetDocumentationComments(this IMember Member, Func<string, CodeBuilder> CommentingFunction)
        {
            return CommentingFunction(Member.GetDocumentation());
        }

        public static CodeBuilder GetDocumentationComments(this IMember Member, Func<DescriptionAttribute, bool> Predicate, Func<string, CodeBuilder> CommentingFunction)
        {
            return CommentingFunction(Member.GetDocumentation(Predicate));
        }

        public static CodeBuilder GetDocumentationLineComments(this IMember Member, string LineCommentPrefix)
        {
            return ToLineComments(Member.GetDocumentation(), LineCommentPrefix);
        }
        public static CodeBuilder GetDocumentationLineComments(this IMember Member, Func<DescriptionAttribute, bool> Predicate, string LineCommentPrefix)
        {
            return ToLineComments(Member.GetDocumentation(Predicate), LineCommentPrefix);
        }

        public static CodeBuilder GetDocumentationBlockComments(this IMember Member, Func<DescriptionAttribute, bool> Predicate, string LeadingDelimiter, string TrailingDelimiter)
        {
            return ToBlockComments(Member.GetDocumentation(Predicate), LeadingDelimiter, TrailingDelimiter);
        }
        public static CodeBuilder GetDocumentationBlockComments(this IMember Member, Func<DescriptionAttribute, bool> Predicate, string LeadingDelimiter, string TrailingDelimiter, string LinePrefix)
        {
            return ToBlockComments(Member.GetDocumentation(Predicate), LeadingDelimiter, TrailingDelimiter, LinePrefix);
        }
        public static CodeBuilder GetDocumentationBlockComments(this IMember Member, string LeadingDelimiter, string TrailingDelimiter)
        {
            return ToBlockComments(Member.GetDocumentation(), LeadingDelimiter, TrailingDelimiter);
        }
        public static CodeBuilder GetDocumentationBlockComments(this IMember Member, string LeadingDelimiter, string TrailingDelimiter, string LinePrefix)
        {
            return ToBlockComments(Member.GetDocumentation(), LeadingDelimiter, TrailingDelimiter, LinePrefix);
        }

        public static IDocumentationFormatter GetDocumentationFormatter(this ICompilerOptions Options, IDocumentationFormatter Default)
        {
            return Options.GetOption<IDocumentationFormatter>("docs-formatter", Default);
        }
        public static IDocumentationFormatter GetDocumentationFormatter(this ICompilerOptions Options)
        {
            return Options.GetDocumentationFormatter(DefaultDocumentationFormatter.Instance);
        }
    }
}
