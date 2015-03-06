using Flame.Compiler;
using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Flame.DSProject
{
    [Serializable]
    [XmlRoot("Compile")]
    public class DSProjectSourceItem : IProjectSourceItem
    {
        public DSProjectSourceItem()
        {
        }
        public DSProjectSourceItem(string SourceIdentifier, string CurrentPath)
        {
            var localUri = new Uri(CurrentPath, UriKind.RelativeOrAbsolute);
            var absUri = new Uri(SourceIdentifier, UriKind.RelativeOrAbsolute);

            var relUri = localUri.MakeRelativeUri(absUri);

            this.SourceIdentifier = relUri.ToString();
        }
        public DSProjectSourceItem(string SourceIdentifier)
        {
            this.SourceIdentifier = SourceIdentifier;
        }

        public ISourceDocument GetSource(string CurrentPath)
        {
            Uri sourceUri = CurrentPath == null ? new Uri(SourceIdentifier, UriKind.Relative) : new Uri(new Uri(CurrentPath), new Uri(SourceIdentifier, UriKind.Relative));
            using (FileStream fs = new FileStream(sourceUri.AbsolutePath, FileMode.Open))
            using (StreamReader reader = new StreamReader(fs))
            {
                return new SourceDocument(reader.ReadToEnd(), SourceIdentifier);
            }
        }

        [XmlAttribute("Include")]
        public string SourceIdentifier { get; set; }

        public string Name
        {
            get { return null; }
        }
    }
}
