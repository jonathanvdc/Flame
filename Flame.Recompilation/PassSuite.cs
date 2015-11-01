using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler;
using Flame.Compiler.Visitors;

namespace Flame.Recompilation
{
    using IMethodPass = IPass<BodyPassArgument, IStatement>;
    using Flame.Compiler.Build;

    public class PassSuite
    {
        public PassSuite(IMethodOptimizer Optimizer)
            : this(Optimizer, new SlimBodyPass(new EmptyPass<BodyPassArgument>()))
        {
        }
        public PassSuite(IMethodOptimizer Optimizer, IMethodPass MethodPass)
        {
            this.Optimizer = Optimizer;
            this.MethodPass = MethodPass;
        }

        public IMethodOptimizer Optimizer { get; private set; }
        public IMethodPass MethodPass { get; private set; }

        public PassSuite PrependPass(IMethodPass Pass)
        {
            return new PassSuite(Optimizer, new AggregateBodyPass(Pass, MethodPass));
        }
        public PassSuite AppendPass(IMethodPass Pass)
        {
            return new PassSuite(Optimizer, new AggregateBodyPass(MethodPass, Pass));
        }

        public IStatement OptimizeBody(AssemblyRecompiler Recompiler, IBodyMethod SourceMethod)
        {
            var metadata = new PassMetadata(Recompiler.GlobalMetadata,
                Recompiler.GetTypeMetadata(SourceMethod.DeclaringType), 
                new RandomAccessOptions());

            var initBody = Optimizer.GetOptimizedBody(SourceMethod);

            return MethodPass.Apply(new BodyPassArgument(Recompiler, metadata, SourceMethod, initBody));
        }

        public static PassSuite CreateDefault(ICompilerLog Log)
        {
            return new PassSuite(Log.GetMethodOptimizer(), 
                new SlimBodyPass(new EmptyPass<BodyPassArgument>())); 
        }
    }
}
