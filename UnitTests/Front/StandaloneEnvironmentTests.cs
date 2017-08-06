using System;
using Flame;
using Flame.Build;
using Flame.Compiler;
using Flame.Compiler.Statements;
using Flame.Compiler.Expressions;
using Flame.Front.Target;
using Loyc.MiniTest;

namespace UnitTests.Front
{
    [TestFixture]
    public class StandaloneEnvironmentTests
    {
        [Test]
        public void GetRootType()
        {
            var env = new StandaloneEnvironment();
            var asm = new DescribedAssembly(new SimpleName("stdlib"), env);
            var systemNs = new DescribedNamespace(new SimpleName("System"), (IAssembly)asm);
            asm.AddNamespace(systemNs);
            var objType = new DescribedType(new SimpleName("Object"), systemNs);
            systemNs.AddType(objType);
            Assert.AreEqual(null, env.RootType);
            env.Configure(asm.CreateBinder());
            Assert.AreEqual(objType, env.RootType);
        }
    }
}