﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Flame.CodeDescription;

namespace Flame.XmlDocs
{
    [Serializable]
    [XmlRoot("member")]
    [XmlType("member")]
    public class MemberDocumentation
    {
        protected MemberDocumentation()
        {
            this.Bucket = new DocumentationBucket();
        }
        public MemberDocumentation(string MemberName)
            : this()
        {
            this.MemberName = MemberName;
        }
        public MemberDocumentation(IMember Member)
            : this(Member.GetXmlDocName())
        {
        }

        [XmlAttribute("name")]
        public string MemberName { get; set; }

        [XmlAnyElement]
        public DocumentationBucket Bucket { get; set; }

        [XmlIgnore]
        public List<DocumentationElement> Elements
        {
            get
            {
                return Bucket.Elements;
            }
            set
            {
                Bucket.Elements = value;
            }
        }

        [XmlIgnore]
        public bool IsEmpty
        {
            get
            {
                return Elements.Count == 0;
            }
        }

        public void AddDescriptionAttribute(DescriptionAttribute Attribute)
        {
            for (int i = 0; i < Elements.Count; i++)
            {
                if (Elements[i].Tag.Equals(Attribute.Tag, StringComparison.InvariantCultureIgnoreCase))
                {
                    Elements.RemoveAt(i);
                }
            }
            Elements.Add(new DocumentationElement(Attribute));
        }

        public bool MatchesMember(IMember Member)
        {
            return Member.GetXmlDocName() == MemberName;
        }

        public IEnumerable<DescriptionAttribute> ToDescriptionAttributes()
        {
            return Elements.Select((item) => item.Description);
        }

        public static MemberDocumentation FromMember(IMember Member)
        {
            var doc = new MemberDocumentation(Member.GetXmlDocName());
            foreach (var item in Member.GetDescriptionAttributes())
            {
                doc.AddDescriptionAttribute(item);
            }
            return doc;
        }
    }
}
