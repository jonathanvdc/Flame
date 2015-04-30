using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public interface IDocumentationCommentBuilder
    {
        CodeBuilder GetDocumentationComments(IMember Member);
    }

    public class DocumentationCommentBuilder : IDocumentationCommentBuilder
    {
        public DocumentationCommentBuilder(IDocumentationProvider Provider, DocumentationCommentBuilder Other)
            : this(Provider, Other.Rewriter, Other.Formatter, Other.Commenter)
        {
        }
        public DocumentationCommentBuilder(IDocumentationProvider Provider, IDocumentationFormatter Formatter, DocumentationCommentBuilder Other)
            : this(Provider, Other.Rewriter, Formatter, Other.Commenter)
        {
        }
        public DocumentationCommentBuilder(IDocumentationProvider Provider, IDocumentationRewriter Rewriter, DocumentationCommentBuilder Other)
            : this(Provider, Rewriter, Other.Formatter, Other.Commenter)
        {
        }
        public DocumentationCommentBuilder(IDocumentationFormatter Formatter, Func<string, CodeBuilder> Commenter)
            : this(DefaultDocumentationProvider.Instance, DefaultDocumentationRewriter.Instance, Formatter, Commenter)
        {
        }
        public DocumentationCommentBuilder(IDocumentationProvider Provider, IDocumentationRewriter Rewriter, Func<string, CodeBuilder> Commenter)
            : this(Provider, Rewriter, BlockDocumentationFormatter.Default, Commenter)
        {
        }
        public DocumentationCommentBuilder(IDocumentationProvider Provider, IDocumentationFormatter Formatter, Func<string, CodeBuilder> Commenter)
            : this(Provider, DefaultDocumentationRewriter.Instance, Formatter, Commenter)
        {
        }
        public DocumentationCommentBuilder(IDocumentationProvider Provider, Func<string, CodeBuilder> Commenter)
            : this(Provider, DefaultDocumentationRewriter.Instance, BlockDocumentationFormatter.Default, Commenter)
        {
        }
        public DocumentationCommentBuilder(Func<string, CodeBuilder> Commenter)
            : this(DefaultDocumentationProvider.Instance, DefaultDocumentationRewriter.Instance, BlockDocumentationFormatter.Default, Commenter)
        {
        }
        public DocumentationCommentBuilder(IDocumentationProvider Provider, IDocumentationRewriter Rewriter, IDocumentationFormatter Formatter, Func<string, CodeBuilder> Commenter)
        {
            this.Provider = Provider;
            this.Rewriter = Rewriter;
            this.Formatter = Formatter;
            this.Commenter = Commenter;
        }

        public IDocumentationProvider Provider { get; private set; }
        public IDocumentationRewriter Rewriter { get; private set; }
        public IDocumentationFormatter Formatter { get; private set; }
        public Func<string, CodeBuilder> Commenter { get; private set; }

        public CodeBuilder GetDocumentationComments(IMember Member)
        {
            return Commenter(Formatter.Format(Rewriter.Rewrite(Provider.GetDescriptionAttributes(Member), Member)));
        }
    }
}
