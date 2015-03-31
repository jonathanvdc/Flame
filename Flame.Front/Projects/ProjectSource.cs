using Flame.Compiler;
using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Projects
{
    public class ProjectSource : IProjectSourceItem
    {
        public ProjectSource(string SourceIdentifier)
        {
            this.SourceIdentifier = SourceIdentifier;
        }

        public string SourceIdentifier { get; private set; }

        public ISourceDocument GetSource(string CurrentPath)
        {
            PathIdentifier sourceUri = CurrentPath == null ? new PathIdentifier(SourceIdentifier) : new PathIdentifier(CurrentPath).GetAbsolutePath(SourceIdentifier);
            using (FileStream fs = new FileStream(sourceUri.AbsolutePath.Path, FileMode.Open))
            using (StreamReader reader = new StreamReader(fs))
            {
                return new SourceDocument(reader.ReadToEnd(), SourceIdentifier);
            }
        }

        public string Name
        {
            get { return "compile"; }
        }
    }
}
