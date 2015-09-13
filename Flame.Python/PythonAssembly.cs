using Flame.Binding;
using Flame.Compiler;
using Flame.Compiler.Build;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class PythonAssembly : IAssemblyBuilder, IMemberNamingAssembly
    {
        public PythonAssembly(string Name, Version AssemblyVersion)
            : this(Name, AssemblyVersion, new DefaultPythonMemberNamer())
        {
        }
        public PythonAssembly(string Name, Version AssemblyVersion, IMemberNamer MemberNamer)
        {
            this.Name = Name;
            this.AssemblyVersion = AssemblyVersion;
            this.RootNamespace = new PythonRootNamespace(this);
            this.MemberNamer = MemberNamer;
        }

        public string Name { get; private set; }
        public Version AssemblyVersion { get; private set; }
        public PythonRootNamespace RootNamespace { get; private set; }
        public IMemberNamer MemberNamer { get; private set; }

        public INamespaceBuilder DeclareNamespace(string Name)
        {
            return RootNamespace.DeclareNamespace(Name);
        }

        public IEnumerable<IType> AllTypes
        {
            get
            {
                return RootNamespace.GetAllTypes();
            }
        }

        public void Save(Stream Stream)
        {
            var cb = RootNamespace.GetCode();
            cb.IndentationString = "    ";
            string code = cb.ToString();
            using (StreamWriter writer = new StreamWriter(Stream))
            {
                writer.Write(code);
            }
        }

        public void Save(IOutputProvider Target)
        {
            if (Target.PreferSingleOutput)
            {
                using (var stream = Target.Create().OpenOutput())
                {
                    Save(stream);
                }
            }
            else
            {
                var mainModule = new AssemblyModule(this);
                foreach (var item in mainModule.TypeModules)
                {
                    using (var stream = Target.Create(item.Name, "py").OpenOutput())
                    {
                        item.Save(stream);
                    }
                }
                using (var stream = Target.Create().OpenOutput())
                {
                    mainModule.SaveManifest(stream);
                }
            }
        }

        public IMethod GetEntryPoint()
        {
            return null;
        }

        public void SetEntryPoint(IMethod Method)
        {
            // Implement this
        }

        public IAssembly Build()
        {
            return this;
        }

        public string FullName
        {
            get { return Name; }
        }

        public IEnumerable<IAttribute> Attributes
        {
            get { return new IAttribute[0]; }
        }

        public IBinder CreateBinder()
        {
            return new NamespaceTreeBinder(PythonEnvironment.Instance, RootNamespace);
        }

        public void Initialize()
        {
            // Do nothing. This back-end does not need `Initialize` to get things done.
        }
    }
}
