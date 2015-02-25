using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.CodeDescription
{
    public sealed class DefaultDocumentationProvider : IDocumentationProvider
    {
        private DefaultDocumentationProvider()
        {

        }

        private static DefaultDocumentationProvider inst;
        public static DefaultDocumentationProvider Instance
        {
            get
            {
                if (inst == null) { inst = new DefaultDocumentationProvider(); }
                return inst;
            }
        }

        public IEnumerable<DescriptionAttribute> GetDescriptionAttributes(IMember Member)
        {
            return Member.GetDescriptionAttributes();
        }
    }
}
