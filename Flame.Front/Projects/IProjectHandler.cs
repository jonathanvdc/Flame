using Flame;
using Flame.Compiler;
using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    public interface IProjectHandler
    {
        IEnumerable<string> Extensions { get; }
        IProject Parse(ProjectPath Path, ICompilerLog Log);
        Task<IAssembly> CompileAsync(IProject Project, CompilationParameters Parameters);
    }
}
