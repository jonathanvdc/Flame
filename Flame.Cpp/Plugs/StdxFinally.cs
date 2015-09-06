using Flame.Build;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cpp.Plugs
{
    public class StdxFinally : PrimitiveBase, ICppMember
    {
        public StdxFinally(StdxNamespace Namespace)
        {
            this.declNs = Namespace;
        }

        public override string Name
        {
            get { return "finally"; }
        }

        public override IEnumerable<IAttribute> Attributes
        {
            get { return new IAttribute[] { new AccessAttribute(AccessModifier.Public), PrimitiveAttributes.Instance.ValueTypeAttribute }; }
        }

        private StdxNamespace declNs;
        public override INamespace DeclaringNamespace { get { return declNs; } }

        protected override IMethod[] CreateMethods()
        {
            var descCtor = new DescribedMethod("finally", this, PrimitiveTypes.Void, false);
            descCtor.IsConstructor = true;
            var descLambda = new DescribedMethod("<lambda>", null, PrimitiveTypes.Void, true);
            descCtor.AddParameter(new DescribedParameter("functor", MethodType.Create(descLambda)));
            return new IMethod[] { descCtor };
        }

        #region C++ specific

        #region Code

        private const string HeaderCode =
@"class finally
{
    std::function<void(void)> Function;
public:
    finally(const std::function<void(void)> &Function) : Function(Function) {}
    ~finally()
    {
        Function();
    }
};";

        #endregion

        public IEnumerable<IHeaderDependency> Dependencies
        {
            get { return new IHeaderDependency[] { StandardDependency.Functional }; }
        }

        public ICppEnvironment Environment
        {
            get { return declNs.Environment; }
        }

        public CodeBuilder GetHeaderCode()
        {
            return new CodeBuilder(HeaderCode);
        }

        public bool HasSourceCode
        {
            get { return false; }
        }

        public CodeBuilder GetSourceCode()
        {
            return new CodeBuilder();
        }

        #endregion
    }
}
