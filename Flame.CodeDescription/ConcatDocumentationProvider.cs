using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public class ConcatDocumentationProvider : IDocumentationProvider
    {
        public ConcatDocumentationProvider(IDocumentationProvider First, IDocumentationProvider Second)
        {
            this.First = First;
            this.Second = Second;
        }

        public IDocumentationProvider First { get; private set; }
        public IDocumentationProvider Second { get; private set; }

        public IEnumerable<DescriptionAttribute> GetDescriptionAttributes(IMember Member)
        {
            return First.GetDescriptionAttributes(Member).Concat(Second.GetDescriptionAttributes(Member));
        }
    }
}
