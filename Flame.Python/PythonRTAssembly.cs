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

        public QualifiedName FullName
        {
            get { return new QualifiedName(Name); }
        }

        public AttributeMap Attributes
        {
            get { return AttributeMap.Empty; }
        }

        public UnqualifiedName Name
        {
            get { return new SimpleName("PortableRT"); }
        }
    }
}
