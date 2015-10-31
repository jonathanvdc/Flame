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
    [XmlRoot("Option")]
    public class DSProjectOption : PixieXmlSerializable, IProjectOptionItem
    {
        public DSProjectOption()
        {
            this.Key = "";
            this.Value = "";
        }
        public DSProjectOption(string Key, string Value)
        {
            this.Key = Key;
            this.Value = Value;
        }

        public string Name
        {
            get { return null; }
        }
         
        [XmlAttribute("Key")]
        public string Key { get; set; }
        [XmlAttribute("Value")]
        public string Value { get; set; }

        public override void Deserialize(IMarkupNode Node)
        {
            Key = Node.Attributes.Get<string>("Key", "");
            Value = Node.Attributes.Get<string>("Value", "");
        }

        public override IMarkupNode Serialize()
        {
            return new MarkupNode("Option", new PredefinedAttributes(new Dictionary<string, object>()
            {
                { "Key", Key },
                { "Value", Value }
            }));
        }
    }
}
