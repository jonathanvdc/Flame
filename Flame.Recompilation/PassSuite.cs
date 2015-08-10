using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flame.Compiler;
using Flame.Compiler.Visitors;

namespace Flame.Recompilation
{
    using IPreStatementPass = IPass<Tuple<IStatement, IMethod>, Tuple<IStatement, IMethod>>;
    using IMethodPass       = IPass<BodyPassArgument, IStatement>;
    using IStatementPass    = IPass<IStatement, IStatement>;

    public class PassSuite
    {
        public PassSuite(IMethodOptimizer Optimizer)
            : this(Optimizer, new EmptyPass<Tuple<IStatement, IMethod>>(), new SlimBodyPass(new EmptyPass<BodyPassArgument>()))
        {
        }
        public PassSuite(IMethodOptimizer Optimizer, IPreStatementPass PreStatementPass, IMethodPass MethodPass)
            : this(Optimizer, new EmptyPass<Tuple<IStatement, IMethod>>(), MethodPass, new EmptyPass<IStatement>())
        {
        }
        public PassSuite(IMethodOptimizer Optimizer, IPreStatementPass PreStatementPass, IMethodPass MethodPass, IStatementPass StatementPass)
        {
            this.Optimizer = Optimizer;
            this.PreStatementPass = PreStatementPass;
            this.MethodPass = MethodPass;
            this.StatementPass = StatementPass;
        }

        public IMethodOptimizer Optimizer { get; private set; }
        public IPreStatementPass PreStatementPass { get; private set; }
        public IMethodPass MethodPass { get; private set; }
        public IStatementPass StatementPass { get; private set; }

        public PassSuite PrependSuite(PassSuite Suite)
        {
            return new PassSuite(Optimizer,
                new AggregatePass<Tuple<IStatement, IMethod>>(Suite.PreStatementPass, PreStatementPass), 
                new AggregateBodyPass(Suite.MethodPass, MethodPass),
                new AggregatePass<IStatement>(Suite.StatementPass, StatementPass));
        }
        public PassSuite AppendSuite(PassSuite Suite)
        {
            return new PassSuite(Optimizer,
                new AggregatePass<Tuple<IStatement, IMethod>>(PreStatementPass, Suite.PreStatementPass),
                new AggregateBodyPass(MethodPass, Suite.MethodPass),
                new AggregatePass<IStatement>(StatementPass, Suite.StatementPass));
        }

        public IStatement RecompileBody(AssemblyRecompiler Recompiler, ITypeBuilder DeclaringType, 
                                        IMethodBuilder Method, IBodyMethod SourceMethod)
        {
            var metadata = new PassMetadata(Recompiler.GlobalMetdata, Recompiler.GetTypeMetadata(DeclaringType), new RandomAccessOptions());

            var initBody = Optimizer.GetOptimizedBody(SourceMethod);
            var preOptBody = PreStatementPass.Apply(new Tuple<IStatement, IMethod>(initBody, SourceMethod)).Item1;

            var bodyStatement = Recompiler.GetStatement(preOptBody, Method);

            var optBody = StatementPass.Apply(MethodPass.Apply(new BodyPassArgument(Recompiler, metadata, DeclaringType, Method, bodyStatement)));

            var targetBody = Method.GetBodyGenerator();
            var block = optBody.Emit(targetBody);
            Recompiler.TaskManager.RunSequential(Method.SetMethodBody, block);

            return optBody;
        }

        public static PassSuite CreateDefault(ICompilerLog Log)
        {
            return new PassSuite(Log.GetMethodOptimizer(), 
                new EmptyPass<Tuple<IStatement, IMethod>>(), 
                new SlimBodyPass(new EmptyPass<BodyPassArgument>()), 
                new EmptyPass<IStatement>()); 
        }
    }
}
