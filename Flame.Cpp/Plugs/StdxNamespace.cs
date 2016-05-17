using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public class StdxNamespace : PrimitiveNamespace
    {
        public StdxNamespace(ICppEnvironment Environment)
        {
            this.Environment = Environment;

            Register(ArraySlice = new StdxArraySlice(this));
            Register(Finally = new StdxFinally(this));
        }

        public override UnqualifiedName Name
        {
            get { return new SimpleName("stdx"); }
        }

        public ICppEnvironment Environment { get; private set; }
        public StdxArraySlice ArraySlice { get; private set; }
        public StdxFinally Finally { get; private set; }
    }
}
