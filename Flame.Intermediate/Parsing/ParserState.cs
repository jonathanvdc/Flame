using Flame.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate.Parsing
{
    public class ParserState
    {
        public ParserState(IRParser Parser, ImmutableHeader Header, IRAssembly Assembly)
        {
            this.Parser = Parser;
            this.Header = Header;
            this.Assembly = Assembly;
            this.cachedBinder = new Lazy<IBinder>(() => new DualBinder(Assembly.CreateBinder(), Header.ExternalBinder));
        }

        public IRParser Parser { get; private set; }
        public ImmutableHeader Header { get; private set; }
        public IRAssembly Assembly { get; private set; }

        private Lazy<IBinder> cachedBinder;
        public IBinder Binder { get { return cachedBinder.Value; } }
    }
}
