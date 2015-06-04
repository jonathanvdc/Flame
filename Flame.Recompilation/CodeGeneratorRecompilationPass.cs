using Flame.Compiler;
using Flame.Compiler.Visitors;
using Flame.Recompilation.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Recompilation
{
    /// <summary>
    /// A recompilation pass based on a code generator.
    /// </summary>
    public class CodeGeneratorRecompilationPass : IPass<RecompilationPassArguments, INode>
    {
        private CodeGeneratorRecompilationPass()
        {

        }

        static CodeGeneratorRecompilationPass()
        {
            Instance = new CodeGeneratorRecompilationPass();
        }

        public static CodeGeneratorRecompilationPass Instance { get; private set; }

        public INode Apply(RecompilationPassArguments Args)
        {
            var codeGen = new RecompiledCodeGenerator(Args.Recompiler, Args.TargetMethod);
            var block = Args.Body.Emit(codeGen);
            return Args.Body is IExpression ? (INode)RecompiledCodeGenerator.GetExpression(block) : (INode)RecompiledCodeGenerator.GetStatement(block);
        }
    }
}
