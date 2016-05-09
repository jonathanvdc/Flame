using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonRTAssembly : IAssembly
    {
        public Version AssemblyVersion
        {
            get { return new Version(1, 0); }
        }

        public IBinder CreateBinder()
        {
            throw new NotImplementedException();
        }

        public IMethod GetEntryPoint()
        {
            return null;
        }

        public string FullName
        {
            get { return Name; }
        }

        public AttributeMap Attributes
        {
            get { return AttributeMap.Empty; }
        }

        public string Name
        {
            get { return "PortableRT"; }
        }
    }
}
