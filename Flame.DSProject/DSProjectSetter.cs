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
    [XmlRoot("Setter")]
    public class DSProjectSetter : PixieXmlSerializable, IProjectItem
    {
        public DSProjectSetter()
        {
            this.Property = "";
            this.Value = "";
        }
        public DSProjectSetter(string Property, string Value)
        {
            this.Property = Property;
            this.Value = Value;
        }

        public string Name
        {
            get { return null; }
        }
         
        [XmlAttribute("Property")]
        public string Property { get; set; }
        [XmlAttribute("Value")]
        public string Value { get; set; }

        public override void Deserialize(MarkupNode Node)
        {
            Property = Node.Attributes.Get<string>("Property", "");
            Value = Node.Attributes.Get<string>("Value", "");
        }

        public override MarkupNode Serialize()
        {
            return new MarkupNode("Setter", new PredefinedAttributes(new Dictionary<string, object>()
            {
                { "Property", Property },
                { "Value", Value }
            }));
        }
    }
}
