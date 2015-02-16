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
    [Serializable]
    [XmlRoot("ItemGroup")]
    public class DSProjectNode : IMutableProjectNode
    {
        public DSProjectNode()
        {
            this.Children = new NodeList();
        }
        public DSProjectNode(IEnumerable<IProjectItem> Children)
        {
            this.Children = new NodeList(Children);
        }
        public DSProjectNode(string Name, IEnumerable<IProjectItem> Children)
        {
            this.Name = Name;
            this.Children = new NodeList(Children);
        }

        [XmlAttribute("Name")]
        public string Name { get; set; }
        [XmlAnyElement]
        public NodeList Children { get; set; }

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
    }
}
