using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class ClassModule : IPythonModule
    {
        public ClassModule(string Name, params PythonClass[] ModuleTypes)
        {
            this.Name = Name;
            this.ModuleTypes = ModuleTypes;
        }
        public ClassModule(string Name, IEnumerable<PythonClass> ModuleTypes)
        {
            this.Name = Name;
            this.ModuleTypes = ModuleTypes;
        }

        public string Name { get; private set; }
        public IEnumerable<PythonClass> ModuleTypes { get; private set; }

        public IEnumerable<IType> GetModuleTypes()
        {
            return ModuleTypes;
        }

        public IEnumerable<ModuleDependency> GetDependencies()
        {
            return ModuleTypes.GetDependencies();
        }

        public CodeBuilder GetCode()
        {
            CodeBuilder cb = new CodeBuilder();
            cb.IndentationString = new string(' ', 4);
            foreach (var item in GetDependencies())
            {
                cb.AddCodeBuilder(item.CreateImportStatement(true));
            }
            if (cb.LineCount > 0)
            {
                cb.AddEmptyLine();
            }
            foreach (var item in ModuleTypes)
            {
                cb.AddCodeBuilder(item.GetCode());
            }
            return cb;
        }

        public void Save(Stream Stream)
        {
            string code = GetCode().ToString();
            using (StreamWriter writer = new StreamWriter(Stream))
            {
                writer.Write(code);
            }
        }
    }
}
