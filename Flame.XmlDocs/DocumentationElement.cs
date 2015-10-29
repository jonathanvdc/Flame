using Pixie;
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
            this.Contents = new MarkupNode("summary");
        }
        public DocumentationElement(IMarkupNode Contents)
        {
            this.Contents = Contents;
        }
        public DocumentationElement(DescriptionAttribute Description)
        {
            this.Description = Description;
        }

        public string Tag { get { return Contents.Type; } }

        /// <summary>
        /// Gets or sets the documentation element's contents.
        /// </summary>
        public IMarkupNode Contents { get; set; }

        /// <summary>
        /// Gets or sets the documentation element's value as a description attribute.
        /// </summary>
        public DescriptionAttribute Description
        {
            get
            {
                return new DescriptionAttribute(Contents);
            }
            set
            {
                this.Contents = value.Contents;
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            Contents = Pixie.Xml.XmlNodeHandler.Instance.ReadMarkupNode(reader);
        }

        public void WriteXml(XmlWriter writer)
        {
            Pixie.Xml.XmlNodeHandler.Instance.WriteMarkupNode(writer, Contents);
        }
    }
}
