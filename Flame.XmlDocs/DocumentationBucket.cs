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
    public class DocumentationBucket
    {
        public DocumentationBucket()
        {
            this.Elements = new List<DocumentationElement>();
        }
        public DocumentationBucket(IEnumerable<IMarkupNode> Elements)
        {
            this.Elements = new List<DocumentationElement>(Elements.Select(item => new DocumentationElement(item)));
        }
        public DocumentationBucket(IEnumerable<DocumentationElement> Elements)
        {
            this.Elements = new List<DocumentationElement>(Elements);
        }

        public List<DocumentationElement> Elements { get; set; }

        public IEnumerable<IMarkupNode> Nodes
        {
            get
            {
                return Elements.Select(item => item.Contents);
            }
        }
    }
}
