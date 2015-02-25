using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public class RewritingDocumentationProvider : IDocumentationProvider
    {
        public RewritingDocumentationProvider(IDocumentationProvider Provider, IDocumentationRewriter Rewriter)
        {
            this.Provider = Provider;
            this.Rewriter = Rewriter;
        }

        public IDocumentationProvider Provider { get; private set; }
        public IDocumentationRewriter Rewriter { get; private set; }

        public IEnumerable<DescriptionAttribute> GetDescriptionAttributes(IMember Member)
        {
            return Rewriter.Rewrite(Provider.GetDescriptionAttributes(Member), Member);
        }
    }
}
