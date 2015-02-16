using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Flame.DSProject
{
    [Serializable]
    [XmlRoot("Reference")]
    public class DSProjectReferenceItem : IProjectReferenceItem
    {
        public DSProjectReferenceItem()
        {

        }
        public DSProjectReferenceItem(string ReferenceIdentifier)
        {
            this.ReferenceIdentifier = ReferenceIdentifier;
        }

        public bool IsRuntimeLibrary { get { return false; } }

        [XmlAttribute("Include")]
        public string ReferenceIdentifier { get; set; }

        public string Name
        {
            get { return null; }
        }
    }
}
