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
    [XmlRoot("Option")]
    public class DSProjectOption : IProjectOptionItem
    {
        public DSProjectOption()
        {
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
    }
}
