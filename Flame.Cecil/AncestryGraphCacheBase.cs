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

        }
        public AncestryGraphCacheBase(AncestryGraph AncestryGraph)
        {
            this.ancestryGraphCache = AncestryGraph;
        }

        private AncestryGraph ancestryGraphCache;
        public AncestryGraph AncestryGraph
        {
            get
            {
                if (ancestryGraphCache == null)
                {
                    ancestryGraphCache = new AncestryGraph();
                }
                return ancestryGraphCache;
            }
        }
    }
}
