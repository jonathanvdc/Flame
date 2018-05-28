using System;
using Loyc.MiniTest;
using Flame.Clr;
using Flame.TypeSystem;

namespace UnitTests.Flame.Clr
{
    /// <summary>
    /// Unit tests that ensure 'Flame.Clr' type resolution works
    /// for intra-assembly references.
    /// </summary>
    [TestFixture]
    public class LocalTypeResolutionTests
    {
        private ClrAssembly mscorlib = new ClrAssembly(
            Mono.Cecil.ModuleDefinition.ReadModule(typeof(object).Module.FullyQualifiedName).Assembly,
            NullAssemblyResolver.Instance);


        [Test]
        public void ResolveTypeSystem()
        {
            // TODO: get this to work!
            // mscorlib.Resolve(mscorlib.Definition.MainModule.TypeSystem.Object);
        }
    }
}

