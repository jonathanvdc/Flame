using Flame.Front.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    public class ProjectPath
    {
        public ProjectPath(PathIdentifier Path, BuildArguments Arguments)
        {
            this.Path = Path;
            this.Arguments = Arguments;
        }

        public PathIdentifier Path { get; private set; }
        public BuildArguments Arguments { get; private set; }

        public bool MakeProject { get { return Arguments.MakeProject; } }

        public string Extension { get { return Path.Extension.TrimStart('.'); } }

        public bool FileExists
        {
            get
            {
                return System.IO.File.Exists(Path.AbsolutePath.Path);
            }
        }

        public bool HasExtension(string Extension)
        {
            return Path.Path.EndsWith("." + Extension, StringComparison.OrdinalIgnoreCase);
        }

        public ProjectPath ChangeExtension(string Extension)
        {
            return new ProjectPath(Path.ChangeExtension(Extension), Arguments);
        }

        public override string ToString()
        {
            return Path.ToString();
        }
    }
}
