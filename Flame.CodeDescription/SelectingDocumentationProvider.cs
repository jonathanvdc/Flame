using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public class SelectingDocumentationProvider : IDocumentationProvider
    {
        public SelectingDocumentationProvider(IDocumentationProvider Provider, Func<DescriptionAttribute, bool> Predicate)
        {
            this.Provider = Provider;
            this.Predicate = Predicate;
        }

        public IDocumentationProvider Provider { get; private set; }
        public Func<DescriptionAttribute, bool> Predicate { get; private set; }

        public IEnumerable<DescriptionAttribute> GetDescriptionAttributes(IMember Member)
        {
            return Provider.GetDescriptionAttributes(Member).Where(Predicate);
        }
    }
}
