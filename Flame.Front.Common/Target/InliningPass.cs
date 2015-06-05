using Flame.Compiler;
using Flame.Compiler.Visitors;
using Flame.Optimization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class InliningPass : IPass<BodyPassArgument, IStatement>
    {
        private InliningPass()
        {

        }

        static InliningPass()
        {
            Instance = new InliningPass();
        }

        public static InliningPass Instance { get; private set; }

        public bool ShouldInline(IBodyPassEnvironment PassEnvironment, DissectedCall Call)
        {
            var body = PassEnvironment.GetMethodBody(Call.Method);
            if (body == null)
            {
                return false;
            }

            return true;
        }

        public IStatement Apply(BodyPassArgument Value)
        {
            var inliner = new InliningVisitor(Value.Method, call => ShouldInline(Value.PassEnvironment, call), Value.PassEnvironment.GetMethodBody);
            return inliner.Visit(Value.Body);
        }
    }
}
