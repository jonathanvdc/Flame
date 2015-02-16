using Flame.Compiler.Projects;
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
    public class DSProjectSetter : IProjectItem
    {
        public DSProjectSetter()
        {
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
    }
}
