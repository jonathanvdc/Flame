using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler;
using Flame.Compiler.Visitors;

namespace Flame.Recompilation
{
    using IMethodPass    = IPass<BodyPassArgument, IStatement>;
    using IStatementPass = IPass<IStatement, IStatement>;

    public class PassSuite
    {
        public PassSuite(IMethodOptimizer Optimizer)
        {
            this.Optimizer = Optimizer;
            this.MethodPass = new SlimBodyPass(new EmptyPass<BodyPassArgument>());
            this.StatementPass = new EmptyPass<IStatement>();
        }
        public PassSuite(IMethodOptimizer Optimizer, IMethodPass MethodPass)
        {
            this.Optimizer = Optimizer;
            this.MethodPass = MethodPass;
            this.StatementPass = new EmptyPass<IStatement>();
        }
        public PassSuite(IMethodOptimizer Optimizer, IMethodPass MethodPass, IStatementPass StatementPass)
        {
            this.Optimizer = Optimizer;
            this.MethodPass = MethodPass;
            this.StatementPass = StatementPass;
        }

        public IMethodOptimizer Optimizer { get; private set; }
        public IMethodPass MethodPass { get; private set; }
        public IStatementPass StatementPass { get; private set; }

        public void RecompileBody(AssemblyRecompiler Recompiler, IEnvironment Environment, ITypeBuilder DeclaringType, 
                                  IMethodBuilder Method, IBodyMethod SourceMethod)
        {
            var initBody = Optimizer.GetOptimizedBody(SourceMethod);

            var bodyStatement = Recompiler.GetStatement(initBody, Method);

            var optBody = StatementPass.Apply(MethodPass.Apply(new BodyPassArgument(Environment, DeclaringType, Method, bodyStatement)));

            var targetBody = Method.GetBodyGenerator();
            var block = optBody.Emit(targetBody);
            Method.SetMethodBody(block);
        }

        public static PassSuite CreateDefault(ICompilerLog Log)
        {
            return new PassSuite(Log.GetMethodOptimizer(), new SlimBodyPass(new EmptyPass<BodyPassArgument>()), new EmptyPass<IStatement>()); 
        }
    }
}
