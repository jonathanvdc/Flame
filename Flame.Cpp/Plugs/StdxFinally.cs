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
        private StdxFinally()
        {

        }

        private static StdxFinally inst;
        public static StdxFinally Instance
        {
            get
            {
                if (inst == null)
                {
                    inst = new StdxFinally();
                }
                return inst;
            }
        }

        public override string Name
        {
            get { return "finally"; }
        }

        public override IEnumerable<IAttribute> GetAttributes()
        {
            return new IAttribute[] { new AccessAttribute(AccessModifier.Public), PrimitiveAttributes.Instance.ValueTypeAttribute };
        }

        public override INamespace DeclaringNamespace
        {
            get { return StdxNamespace.Instance; }
        }

        public override IMethod[] GetConstructors()
        {
            var descCtor = new DescribedMethod("finally", this, PrimitiveTypes.Void, false);
            descCtor.IsConstructor = true;
            var descLambda = new DescribedMethod("<lambda>", null, PrimitiveTypes.Void, true);
            descCtor.AddParameter(new DescribedParameter("functor", MethodType.Create(descLambda)));
            return new IMethod[] { descCtor };
        }

        public override IField[] GetFields()
        {
            return new IField[0];
        }

        public override IMethod[] GetMethods()
        {
            return new IMethod[0];
        }

        public override IProperty[] GetProperties()
        {
            return new IProperty[0];
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
            get { return new CppEnvironment(); }
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
