using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    public class RecompiledNamespaceTemplate : RecompiledMemberTemplate, INamespace
    {
        public RecompiledNamespaceTemplate(AssemblyRecompiler Recompiler, INamespace SourceNamespace)
            : base(Recompiler)
        {
            this.SourceNamespace = SourceNamespace;
        }

        public INamespace SourceNamespace { get; private set; }

        public override IMember GetSourceMember()
        {
            return SourceNamespace;
        }

        public IAssembly DeclaringAssembly
        {
            get { return Recompiler.TargetAssembly; }
        }

        public IEnumerable<IType> Types
        {
            get { return Recompiler.GetTypes(SourceNamespace.Types); }
        }
    }
}
