using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    public class IRRootNamespace : IRNamespaceBase
    {
        public IRRootNamespace(IAssembly DeclaringAssembly)
        {
            this.declAsm = DeclaringAssembly;
        }
        public IRRootNamespace(IAssembly DeclaringAssembly, INodeStructure<IEnumerable<IType>> TypeNodes, INodeStructure<IEnumerable<INamespaceBranch>> NamespaceNodes)
            : base(TypeNodes, NamespaceNodes)
        {
            this.declAsm = DeclaringAssembly;
        }

        private IAssembly declAsm;
        public override IAssembly DeclaringAssembly
        {
            get
            {
                return declAsm;
            }
        }

        public override AttributeMap Attributes
        {
            get { return DeclaringAssembly.Attributes; }
        }

        public override string FullName
        {
            get { return Name; }
        }

        public override string Name
        {
            get { return ""; }
        }
    }
}
