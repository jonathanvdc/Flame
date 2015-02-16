using Flame.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Python
{
    public class ModuleDependency : IEquatable<ModuleDependency>
    {
        public ModuleDependency(IPythonModule Module)
        {
            this.Module = Module;
        }

        public IPythonModule Module { get; private set; }

        public CodeBuilder CreateImportStatement(bool ImportAll)
        {
            if (ImportAll)
            {
                return new CodeBuilder("from " + Module.Name + " import *");
            }
            else
            {
                return new CodeBuilder("import " + Module.Name);
            }
        }

        public static IEnumerable<ModuleDependency> FromType(IType Type)
        {
            if (Type is PythonClass)
            {
                return new ModuleDependency[] { new ModuleDependency(new ClassModule(Type.Name, (PythonClass)Type)) };
            }
            else
            {
                return Enumerable.Empty<ModuleDependency>();
            }
        }

        public override int GetHashCode()
        {
            return this.Module.Name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is ModuleDependency)
            {
                return this == (ModuleDependency)obj;
            }
            return base.Equals(obj);
        }

        public bool Equals(ModuleDependency other)
        {
            return this.Module.Name == other.Module.Name;
        }

        public static bool operator==(ModuleDependency Left, ModuleDependency Right)
        {
            return Left.Module.Name == Right.Module.Name;
        }
        public static bool operator !=(ModuleDependency Left, ModuleDependency Right)
        {
            return !(Left == Right);
        }
    }
}
