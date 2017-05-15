using Flame.CodeDescription;
using Flame.Compiler;
using Flame.Compiler.Build;
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
        [XmlArrayItem("member", typeof(MemberDocumentation))]
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

        public void AddDescription(IMember Member)
        {
            var doc = MemberDocumentation.FromMember(Member);
            if (!doc.IsEmpty)
            {
                this.Members.Add(doc);
            }
        }

        public void AddContentDescriptions(IAssembly Assembly)
        {
            foreach (var item in Assembly.CreateBinder().GetTypes())
            {
                AddDescription(item);
                foreach (var typeMember in item.Methods.Concat<ITypeMember>(item.Properties).Concat<ITypeMember>(item.Fields))
                {
                    AddDescription(typeMember);
                }
            }
        }

        #region IO

        public void Save(Stream Target)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(XmlDocumentationProvider), new Type[] 
            { 
                typeof(MemberDocumentation),
                typeof(AssemblyDocumentation),
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
            return FromAssemblies(Assembly, new IAssembly[] { Assembly });
        }

        /// <summary>
        /// Creates an XML documentation provider from the given assemblies.
        /// </summary>
        /// <param name="TargetAssembly">
        /// The target assembly, which is the result of linking the documented assemblies.
        /// Its signature used to generate documentation, but its contents are not.</param>
        /// <param name="SourceAssemblies">
        /// The assemblies which are linked into the target assembly. Their contents (but not
        /// their signatures) are used to generate documentation.
        /// </param>
        /// <returns>An XML documentation provider.</returns>
        public static XmlDocumentationProvider FromAssemblies(
            IAssembly TargetAssembly,
            IEnumerable<IAssembly> SourceAssemblies)
        {
            XmlDocumentationProvider provider = new XmlDocumentationProvider();
            provider.Assembly = AssemblyDocumentation.FromAssembly(TargetAssembly);
            foreach (var asm in SourceAssemblies)
            {
                provider.AddContentDescriptions(asm);
            }

            return provider;
        }

        #endregion
    }
}
