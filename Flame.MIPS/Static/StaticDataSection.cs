using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.MIPS.Static
{
    public class StaticDataSection
    {
        public StaticDataSection()
        {
            this.items = new List<IStaticDataItem>();
        }

        private List<IStaticDataItem> items;

        public void AddItem(IStaticDataItem Item)
        {
            this.items.Add(Item);
        }

        public IEnumerable<IStaticDataItem> GetItems()
        {
            return items;
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            foreach (var item in items)
            {
                cb.AddCodeBuilder(item.GetCode());
            }
            return cb;
        }
    }
}
