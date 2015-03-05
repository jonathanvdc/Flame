using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public class ConstantDocumentationProvider : IDocumentationProvider
    {
        public ConstantDocumentationProvider(IEnumerable<DescriptionAttribute> DescriptionAttributes)
        {
            this.DescriptionAttributes = DescriptionAttributes;
        }

        public IEnumerable<DescriptionAttribute> DescriptionAttributes { get; private set; }

        public IEnumerable<DescriptionAttribute> GetDescriptionAttributes(IMember Member)
        {
            return DescriptionAttributes;
        }
    }
}
