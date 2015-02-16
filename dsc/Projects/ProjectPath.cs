using dsc.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsc.Projects
{
    public class ProjectPath
    {
        public ProjectPath(string Path, BuildArguments Arguments)
        {
            this.Path = Path;
            this.Arguments = Arguments;
        }

        public string Path { get; private set; }
        public BuildArguments Arguments { get; private set; }

        public bool MakeProject { get { return Arguments.MakeProject; } }
        public IReadOnlyDictionary<string, string> Options { get { return Arguments.GetCompilerOptions(); } }

        public string Extension { get { return System.IO.Path.GetExtension(Path).Replace(".", ""); } }

        public bool FileExists
        {
            get
            {
                return System.IO.File.Exists(Path);
            }
        }

        public bool HasExtension(string Extension)
        {
            return Path.EndsWith("." + Extension);
        }

        public ProjectPath ChangeExtension(string Extension)
        {
            return new ProjectPath(System.IO.Path.ChangeExtension(Path, Extension), Arguments);
        }
    }
}
