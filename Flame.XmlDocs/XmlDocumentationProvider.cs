using Flame.CodeDescription;
using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Flame.XmlDocs
{
    [XmlRoot("doc")]
    [Serializable]
    public class XmlDocumentationProvider : IDocumentationProvider, IDocumentationBuilder
    {
        public XmlDocumentationProvider()
        {
            this.Assembly = new AssemblyDocumentation();
            this.Members = new List<MemberDocumentation>();
        }

        [XmlElement("assembly")]
        public AssemblyDocumentation Assembly { get; set; }

        [XmlArray("members")]
        [XmlArrayItem(typeof(MemberDocumentation))]
        public List<MemberDocumentation> Members { get; set; }

        public MemberDocumentation GetMemberDocumentation(IMember Member)
        {
            return Members.FirstOrDefault((item) => item.MatchesMember(Member));
        }

        public IEnumerable<DescriptionAttribute> GetDescriptionAttributes(IMember Member)
        {
            return Members.Where((item) => item.MatchesMember(Member)).SelectMany((item) => item.ToDescriptionAttributes());
        }

        public void AddDescriptionAttribute(IMember Member, DescriptionAttribute Attribute)
        {
            var memberDocs = GetMemberDocumentation(Member);
            if (memberDocs == null)
            {
                memberDocs = new MemberDocumentation(Member);
            }
            memberDocs.AddDescriptionAttribute(Attribute);
        }

        protected void AddDescription(IMember Member)
        {
            var doc = MemberDocumentation.FromMember(Member);
            if (!doc.IsEmpty)
            {
                this.Members.Add(doc);
            }
        }

        #region IO

        public void Save(Stream Target)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(XmlDocumentationProvider), new Type[] 
            { 
                typeof(MemberDocumentation),
                typeof(AssemblyDocumentation),
                typeof(DocumentationElement),
                typeof(DocumentationBucket),
                typeof(List<MemberDocumentation>)                
            });
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            serializer.Serialize(Target, this, ns);
        }

        public string Extension
        {
            get { return "xml"; }
        }

        public void Save(IOutputProvider Target)
        {
            using (var stream = Target.Create().OpenOutput())
            {
                Save(stream);
            }
        }

        #endregion

        #region Static

        public static XmlDocumentationProvider FromAssembly(IAssembly Assembly)
        {
            XmlDocumentationProvider provider = new XmlDocumentationProvider();
            provider.Assembly = AssemblyDocumentation.FromAssembly(Assembly);
            foreach (var item in Assembly.CreateBinder().GetTypes())
            {
                provider.AddDescription(item);
                foreach (var typeMember in item.GetMethods().Concat<ITypeMember>(item.GetProperties()).Concat<ITypeMember>(item.GetFields()))
                {
                    provider.AddDescription(typeMember);
                }
            }
            return provider;
        }

        #endregion
    }
}
