using Flame.Compiler.Projects;
using Pixie;
using Pixie.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Flame.DSProject
{
    [Serializable]
    [XmlRoot("RuntimeLibrary")]
    public class DSProjectRuntimeLibrary : PixieXmlSerializable, IProjectReferenceItem
    {
        public DSProjectRuntimeLibrary()
        {
            this.ReferenceIdentifier = "";
        }
        public DSProjectRuntimeLibrary(string ReferenceIdentifier)
        {
            this.ReferenceIdentifier = ReferenceIdentifier;
        }

        public bool IsRuntimeLibrary { get { return true; } }

        [XmlAttribute("Include")]
        public string ReferenceIdentifier { get; set; }

        public string Name
        {
            get { return null; }
        }

        public override void Deserialize(MarkupNode Node)
        {
            ReferenceIdentifier = Node.Attributes.Get<string>("Include", "");
        }

        public override MarkupNode Serialize()
        {
            return new MarkupNode("RuntimeLibrary", new PredefinedAttributes(new Dictionary<string, object>()
            {
                { "Include", ReferenceIdentifier }
            }));
        }
    }
}
