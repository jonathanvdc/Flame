using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Flame.XmlDocs
{
    [Serializable]
    [XmlRoot("assembly")]
    public class AssemblyDocumentation
    {
        public AssemblyDocumentation()
        {

        }
        public AssemblyDocumentation(string Name)
        {
            this.Name = Name;
        }

        [XmlElement("name")]
        public string Name { get; set; }

        public static AssemblyDocumentation FromAssembly(IAssembly Assembly)
        {
            return new AssemblyDocumentation(Assembly.Name);
        }
    }
}
