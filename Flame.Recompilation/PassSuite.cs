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

        public IStatement RecompileBody(AssemblyRecompiler Recompiler, IMethod InputMethod, IMethodBuilder OutputMethod, IBodyMethod SourceMethod)
        {
            var metadata = new PassMetadata(Recompiler.GlobalMetdata, Recompiler.GetTypeMetadata(InputMethod.DeclaringType), new RandomAccessOptions());

            var initBody = Optimizer.GetOptimizedBody(SourceMethod);

            var optBody = MethodPass.Apply(new BodyPassArgument(Recompiler, metadata, InputMethod, initBody));

            var bodyStatement = Recompiler.GetStatement(optBody, InputMethod);

            var targetBody = OutputMethod.GetBodyGenerator();
            var block = optBody.Emit(targetBody);
            Recompiler.TaskManager.RunSequential(OutputMethod.SetMethodBody, block);

            return optBody;
        }

        public static PassSuite CreateDefault(ICompilerLog Log)
        {
            return new PassSuite(Log.GetMethodOptimizer(), 
                new SlimBodyPass(new EmptyPass<BodyPassArgument>())); 
        }
    }
}
