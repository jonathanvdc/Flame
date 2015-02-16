using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public class DefaultDocumentationProvider : IDocumentationProvider
    {
        public IEnumerable<DescriptionAttribute> GetDescriptionAttributes(IMember Member)
        {
            return Member.GetDescriptionAttributes();
        }
    }
}
