using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class AssemblyModule : IPythonModule
    {
        public AssemblyModule(PythonAssembly Assembly)
        {
            this.Assembly = Assembly;
        }

        public PythonAssembly Assembly { get; private set; }

        private ClassModule[] tModules;
        public IEnumerable<ClassModule> TypeModules
        {
            get
            {
                if (tModules == null)
                {
                    tModules = Assembly.AllTypes.OfType<PythonClass>().Select((item) => new ClassModule(item.Name.ToString(), item)).ToArray();
                }
                return tModules;
            }
        }

        public string Name
        {
            get { return Assembly.Name.ToString(); }
        }

        public IEnumerable<IType> GetModuleTypes()
        {
            return Assembly.AllTypes;
        }

        public IEnumerable<ModuleDependency> GetTypeModuleDependencies()
        {
            return TypeModules.Select((item) => new ModuleDependency(item));
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return Assembly.AllTypes.OfType<IDependencyNode>().GetDependencies().Except(GetTypeModuleDependencies());
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.IndentationString = new string(' ', 4);
            foreach (var item in TypeModules)
            {
                cb.AddCodeBuilder(item.GetCode());
            }
            return cb;
        }

        public CodeBuilder GetManifestCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.IndentationString = new string(' ', 4);
            foreach (var item in GetTypeModuleDependencies())
            {
                cb.AddCodeBuilder(item.CreateImportStatement(true));
            }
            return cb;
        }

        public void SaveManifest(Stream Target)
        {
            string code = GetManifestCode().ToString();
            using (StreamWriter writer = new StreamWriter(Target))
            {
                writer.Write(code);
            }
        }
    }
}
