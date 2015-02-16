using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Flame.DSProject
{
    public class NodeList : IList<IProjectItem>, IXmlSerializable
    {
        public NodeList()
        {
            this.Nodes = new List<IProjectItem>();
        }
        public NodeList(IEnumerable<IProjectItem> Items)
        {
            this.Nodes = new List<IProjectItem>(Items);
        }

        public List<IProjectItem> Nodes { get; private set; }

        #region Serialization

        public System.Xml.Schema.XmlSchema GetSchema() { return null; }

        public void ReadXml(XmlReader reader)
        {
            while (reader.IsStartElement())
            {
                Type serializedType;
                switch (reader.Name)
                {
                    case "Setter":
                        serializedType = typeof(DSProjectSetter);
                        break;
                    case "Option":
                        serializedType = typeof(DSProjectOption);
                        break;
                    case "RuntimeLibrary":
                        serializedType = typeof(DSProjectRuntimeLibrary);
                        break;
                    case "Reference":
                        serializedType = typeof(DSProjectReferenceItem);
                        break;
                    case "Compile":
                        serializedType = typeof(DSProjectSourceItem);
                        break;
                    case "ItemGroup":
                        serializedType = typeof(DSProjectNode);
                        break;
                    case "Project":
                        serializedType = typeof(DSProject);
                        break;
                    default:
                        throw new NotSupportedException("Node type '" + reader.Name + "' is not supported.");
                }

                XmlSerializer serializer = new XmlSerializer(serializedType);
                Nodes.Add((IProjectItem)serializer.Deserialize(reader));
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            foreach (var child in Nodes)
            {
                XmlSerializer xmlSerializer = new XmlSerializer(child.GetType());
                xmlSerializer.Serialize(writer, child);
            }
        }

        #endregion

        #region IList<IProjectItem> Implementation

        public int IndexOf(IProjectItem item)
        {
            return Nodes.IndexOf(item);
        }

        public void Insert(int index, IProjectItem item)
        {
            Nodes.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Nodes.RemoveAt(index);
        }

        public IProjectItem this[int index]
        {
            get
            {
                return Nodes[index];
            }
            set
            {
                Nodes[index] = value;
            }
        }

        public void Add(IProjectItem item)
        {
            Nodes.Add(item);
        }

        public void Clear()
        {
            Nodes.Clear();
        }

        public bool Contains(IProjectItem item)
        {
            return Nodes.Contains(item);
        }

        public void CopyTo(IProjectItem[] array, int arrayIndex)
        {
            Nodes.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return Nodes.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(IProjectItem item)
        {
            return Nodes.Remove(item);
        }

        public IEnumerator<IProjectItem> GetEnumerator()
        {
            return Nodes.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
