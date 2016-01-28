using Flame.Compiler.Projects;
using Pixie;
using Pixie.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Flame.DSProject
{
    [XmlRoot("ItemGroup")]
    public class DSProjectNode : PixieXmlSerializable, IMutableProjectNode
    {
        public DSProjectNode()
        {
            this.Children = new List<IProjectItem>();
        }
        public DSProjectNode(IEnumerable<IProjectItem> Children)
        {
            this.Children = new List<IProjectItem>(Children);
        }
        public DSProjectNode(string Name, IEnumerable<IProjectItem> Children)
        {
            this.Name = Name;
            this.Children = new List<IProjectItem>(Children);
        }
        public DSProjectNode(MarkupNode Node)
        {
            Deserialize(Node);
        }

        [XmlIgnore]
        public string Name { get; set; }

        [XmlAttribute("Name")]
        public string XmlName
        {
            get
            {
                if (string.IsNullOrEmpty(Name))
                {
                    return null;
                }
                else
                {
                    return Name;
                }
            }
            set
            {
                Name = value;
            }
        }

        public List<IProjectItem> Children { get; set; }

        public void SetName(string Name)
        {
            this.Name = Name;
        }

        #region Settings

        public string GetSetting(string Property, string DefaultValue = null)
        {
            foreach (var item in Children)
            {
                if (item is DSProjectSetter)
                {
                    var setter = (DSProjectSetter)item;
                    if (setter.Property == Property)
                    {
                        return setter.Value;
                    }
                }
            }
            return DefaultValue;
        }
        public void SetSetting(string Property, string Value)
        {
            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i] is DSProjectSetter)
                {
                    var setter = (DSProjectSetter)Children[i];
                    if (setter.Property == Property)
                    {
                        Children[i] = new DSProjectSetter(Property, Value);
                        return;
                    }
                }
            }
            Children.Add(new DSProjectSetter(Property, Value));
        }

        #endregion

        public void AddChild(IProjectItem Item)
        {
            this.Children.Add(Item);
        }

        public IProjectItem[] GetChildren()
        {
            return Children.ToArray();
        }

        public override void Deserialize(MarkupNode Node)
        {
            XmlName = Node.Attributes.Get<string>("Name", "");
            Children = new List<IProjectItem>();
            foreach (var item in Node.Children)
            {
                PixieXmlSerializable result;
                switch (item.Type)
                {
                    case "Setter":
                        result = new DSProjectSetter();
                        break;
                    case "Option":
                        result = new DSProjectOption();
                        break;
                    case "RuntimeLibrary": 
                        result = new DSProjectRuntimeLibrary();
                        break;
                    case "Reference":
                        result = new DSProjectReferenceItem();
                        break;
                    case "Compile":
                        result = new DSProjectSourceItem();
                        break;
                    case "ItemGroup":
                        result = new DSProjectNode();
                        break;
                    case "Project":
                        result = new DSProject();
                        break;
                    default:
                        throw new NotSupportedException("Node type '" + item.Type + "' is not supported.");
                }
                result.Deserialize(item);
                Children.Add((IProjectItem)result);
            }
        }

        public override MarkupNode Serialize()
        {
            return new MarkupNode("ItemGroup", new PredefinedAttributes(new Dictionary<string, object>()
            {
                { "Name", "" }
            }), Children.Cast<PixieXmlSerializable>().Select(item => item.Serialize()).ToArray());
        }
    }
}
