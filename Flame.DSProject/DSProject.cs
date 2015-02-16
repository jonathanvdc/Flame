using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Flame.DSProject
{
    [Serializable]
    [XmlRoot("Project")]
    public class DSProject : DSProjectNode, IMutableProject
    {
        public DSProject()
        {
        }
        public DSProject(string Name, IEnumerable<IProjectItem> Children)
            : base(Name, Children)
        {
        }
        public DSProject(DSProjectNode Node)
            : base(Node.Name, Node.Children)
        {
        }

        [XmlIgnore]
        public string AssemblyName
        {
            get { return GetSetting("AssemblyName", Name); }
            set { SetSetting("AssemblyName", value); }
        }

        [XmlIgnore]
        public string BuildTargetIdentifier
        {
            get { return GetSetting("TargetPlatform", "CLR/Release"); }
            set { SetSetting("TargetPlatform", value); }
        }

        public void SetAssemblyName(string Name)
        {
            AssemblyName = Name;
        }

        public void SetBuildTargetIdentifier(string Identifier)
        {
            BuildTargetIdentifier = Identifier;
        }

        public IProjectReferenceItem CreateReferenceItem(string ReferenceIdentifier, bool IsRuntimeLibrary)
        {
            if (IsRuntimeLibrary)
            {
                return new DSProjectRuntimeLibrary(ReferenceIdentifier);
            }
            else
            {
                return new DSProjectReferenceItem(ReferenceIdentifier);
            }
        }

        public IProjectSourceItem CreateSourceItem(string SourcePath, string CurrentPath)
        {
            if (CurrentPath == null)
            {
                return new DSProjectSourceItem(SourcePath);
            }
            else
            {
                return new DSProjectSourceItem(SourcePath, CurrentPath);
            }
        }

        public IProjectOptionItem CreateOptionItem(string Key, string Value)
        {
            return new DSProjectOption(Key, Value);
        }

        public IMutableProjectNode CreateNode()
        {
            return new DSProjectNode();
        }

        public static DSProject ReadProject(string Path)
        {
            using (var fs = new FileStream(Path, FileMode.Open))
            {
                return DSProject.ReadProject(fs);
            }
        }
        public static DSProject ReadProject(Stream Source)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(DSProject));
            return (DSProject)serializer.Deserialize(Source);
        }

        public void WriteTo(string Path)
        {
            using (var fs = new FileStream(Path, FileMode.Create))
            {
                WriteTo(fs);
            }
        }
        public void WriteTo(Stream Target)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(DSProject));
            var ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            serializer.Serialize(Target, this);
        }

        #region FromProject

        public static DSProject FromProject(IProject Project, string CurrentPath)
        {
            var dsProj = new DSProject();
            Project.CopyTo(dsProj, CurrentPath);
            return dsProj;
        }

        #endregion
    }
}
