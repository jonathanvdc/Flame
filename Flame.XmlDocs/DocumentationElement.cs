using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Flame.XmlDocs
{
    public class DocumentationElement : IXmlSerializable
    {
        public DocumentationElement()
        {
            this.Attributes = new Dictionary<string, string>();
        }
        public DocumentationElement(string Tag, string Contents)
        {
            this.Tag = Tag;
            this.Attributes = new Dictionary<string, string>();
            this.Contents = Contents;
        }
        public DocumentationElement(DescriptionAttribute Description)
        {
            this.Description = Description;
        }

        /// <summary>
        /// Gets or sets the documentation element's tag.
        /// </summary>
        public string Tag { get; set; }
        /// <summary>
        /// Gets or sets a dictionary describing the documentation element's attributes.
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; }
        /// <summary>
        /// Gets or sets the documentation element's contents.
        /// </summary>
        public string Contents { get; set; }

        /// <summary>
        /// Gets or sets the documentation element's value as a description attribute.
        /// </summary>
        public DescriptionAttribute Description
        {
            get
            {
                return new DescriptionAttribute(Tag, Contents, Attributes);
            }
            set
            {
                this.Tag = value.Tag;
                this.Contents = value.Description;
                this.Attributes = new Dictionary<string, string>();
                foreach (var item in value.Attributes)
                {
                    this.Attributes[item.Key] = item.Value;
                }
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            Tag = reader.Name;
            while (reader.Read() && reader.NodeType == XmlNodeType.Attribute)
            {
                string key = reader.LocalName;
                string val = reader.Value;
            }
            Contents = reader.ReadInnerXml();
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Tag);
            foreach (var item in this.Attributes)
            {
                writer.WriteAttributeString(item.Key, item.Value);
            }
            writer.WriteRaw(Contents);
            writer.WriteEndElement();
        }
    }
}
