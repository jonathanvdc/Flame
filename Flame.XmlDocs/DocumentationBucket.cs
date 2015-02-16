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
    public class DocumentationBucket : IXmlSerializable
    {
        public DocumentationBucket()
        {
            this.Elements = new List<DocumentationElement>();
        }
        public DocumentationBucket(List<DocumentationElement> Elements)
        {
            this.Elements = Elements;
        }

        public List<DocumentationElement> Elements { get; set; }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            Elements = new List<DocumentationElement>();
            while (reader.IsStartElement())
            {
                var elem = new DocumentationElement();
                elem.ReadXml(reader);
                Elements.Add(elem);
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var item in Elements)
            {
                item.WriteXml(writer);
            }
        }
    }
}
