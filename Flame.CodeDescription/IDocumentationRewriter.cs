using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public interface IDocumentationRewriter
    {
        IEnumerable<DescriptionAttribute> Rewrite(IEnumerable<DescriptionAttribute> Attributes, IMember Member);
    }
}
