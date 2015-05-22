using Flame;
using Flame.Compiler;
using Flame.Compiler.Projects;
using Flame.Front.Target;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    public struct ParsedProject
    {
        public ParsedProject(PathIdentifier CurrentPath, IProject Project)
        {
            this = default(ParsedProject);
            this.CurrentPath = CurrentPath;
            this.Project = Project;
        }

        public PathIdentifier CurrentPath { get; private set; }
        public IProject Project { get; private set; }
    }

    public interface IProjectHandler
    {
        IEnumerable<string> Extensions { get; }
        IProject Parse(ProjectPath Path, ICompilerLog Log);

        /// <summary>
        /// "Partitions" the given projects, unifying zero or more of them.
        /// </summary>
        /// <param name="Projects"></param>
        /// <returns></returns>
        IEnumerable<ParsedProject> Partition(IEnumerable<ParsedProject> Projects);

        Task<IAssembly> CompileAsync(IProject Project, CompilationParameters Parameters);

        IProject MakeProject(IProject Project, ProjectPath Path, ICompilerLog Log);

        /// <summary>
        /// Gets the project handler's pass preferences.
        /// </summary>
        PassPreferences GetPassPreferences(ICompilerLog Log);
    }
}
