using Flame;
using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Projects
{
    public interface IProjectHandler
    {
        IEnumerable<string> Extensions { get; }
        IProject Parse(ProjectPath Path);
        Task<IAssembly> CompileAsync(IProject Project, CompilationParameters Parameters);
    }
}
