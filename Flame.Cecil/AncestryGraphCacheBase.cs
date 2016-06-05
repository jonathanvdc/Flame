using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class AncestryGraphCacheBase
    {
        public AncestryGraphCacheBase()
        {
            this.ancestryGraphCache = new Lazy<AncestryGraph>(() => new AncestryGraph());
        }
        public AncestryGraphCacheBase(AncestryGraph AncestryGraph)
        {
            this.ancestryGraphCache = new Lazy<AncestryGraph>(() => AncestryGraph);
        }

        private Lazy<AncestryGraph> ancestryGraphCache;
        public AncestryGraph AncestryGraph
        {
            get
            {
                return ancestryGraphCache.Value;
            }
        }
    }
}
