using Pixie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Flame.XmlDocs
{
    public static class MarkupNodeSerializer
    {
        #region Reading

        public static IAttributes ToMarkupAttributes(XmlAttributeCollection Attributes)
        {
            var dict = new Dictionary<string, object>();
            foreach (XmlAttribute item in Attributes)
            {
                dict[item.Name] = item.Value;
            }
            return new PredefinedAttributes(dict);
        }

        public static IEnumerable<IMarkupNode> ToMarkupNodes(XmlNodeList Nodes)
        {
            return ToMarkupNodes(Nodes.Cast<XmlNode>());
        }

        public static IEnumerable<IMarkupNode> ToMarkupNodes(IEnumerable<XmlNode> Nodes)
        {
            return Nodes.Select(ToMarkupNode)
                        .Where(item => item != null);
        }

        public static IMarkupNode CreateMarkupNode(string Tag, XmlAttributeCollection Attributes, IEnumerable<IMarkupNode> Children)
        {
            return new MarkupNode(Tag,
                                  ToMarkupAttributes(Attributes),
                                  Children);
        }

        public static IMarkupNode ToMarkupNode(XmlElement Element)
        {
            return CreateMarkupNode(Element.LocalName, Element.Attributes, ToMarkupNodes(Element.ChildNodes));
        }

        public static IMarkupNode ToMarkupNode(XmlText Element)
        {
            return new MarkupNode(NodeConstants.TextNodeType, Element.Value);
        }

        public static IMarkupNode ToMarkupNode(XmlNode Element)
        {
            if (Element.NodeType == XmlNodeType.Element)
            {
                return ToMarkupNode((XmlElement)Element);
            }
            else if (Element.NodeType == XmlNodeType.Text)
            {
                return ToMarkupNode((XmlText)Element);
            }
            else
            {
                var children = ToMarkupNodes(Element.ChildNodes);
                if (children.Any())
                {
                    return CreateMarkupNode(Element.LocalName, Element.Attributes, children);
                }
                else
                {
                    return null;
                }
            }
        }

        public static IMarkupNode ReadMarkupNode(XmlReader reader)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);
            XmlElement el = doc.DocumentElement;
            return ToMarkupNode(el);
        }

        #endregion

        #region Writing

        public static void WriteMarkupNode(XmlWriter Writer, IMarkupNode Node)
        {
            if (Node.get_IsTextNode())
            {
                Writer.WriteString(Node.GetText());
            }
            else if (Node.Type == "#group")
            {
                foreach (var item in Node.Children)
                {
                    WriteMarkupNode(Writer, item);
                }
            }
            else
            {
                Writer.WriteStartElement(Node.Type);
                
                var attrs = Node.Attributes;
                foreach (var item in attrs.Keys)
	            {
                    string val = attrs.Get<string>(item, "");

                    if (!string.IsNullOrEmpty(val))
                    {
                        Writer.WriteAttributeString(item, val);
                    }
	            }

                foreach (var item in Node.Children)
                {
                    WriteMarkupNode(Writer, item);
                }

                Writer.WriteEndElement();
            }
        }

        #endregion
    }
}
