using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Pixie;
using Pixie.Xml;

namespace Flame.DSProject
{
    [Serializable]
    [XmlRoot("Reference")]
    public class DSProjectReferenceItem : PixieXmlSerializable, IProjectReferenceItem
    {
        public DSProjectReferenceItem()
        {
            this.ReferenceIdentifier = "";
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

        public override void Deserialize(MarkupNode Node)
        {
            ReferenceIdentifier = Node.Attributes.Get<string>("Include", "");
        }

        public override MarkupNode Serialize()
        {
            return new MarkupNode("Reference", new PredefinedAttributes(new Dictionary<string, object>()
            {
                { "Include", ReferenceIdentifier }
            }));
        }
    }
}
