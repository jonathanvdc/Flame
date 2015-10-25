using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Cecil
{
    public class SpecificAssemblyResolver : Mono.Cecil.BaseAssemblyResolver
    {
        public SpecificAssemblyResolver()
        {
            this.cachedDefs = new Dictionary<string, Mono.Cecil.AssemblyDefinition>();
            foreach (var item in GetSearchDirectories())
            {
                RemoveSearchDirectory(item);
            }
        }

        private Dictionary<string, Mono.Cecil.AssemblyDefinition> cachedDefs;

        public void AddAssembly(Mono.Cecil.AssemblyDefinition Definition)
        {
            cachedDefs[Definition.Name.Name] = Definition;
        }

        public override Mono.Cecil.AssemblyDefinition Resolve(Mono.Cecil.AssemblyNameReference name)
        {
            return Resolve(name, new Mono.Cecil.ReaderParameters() 
            { 
                AssemblyResolver = this, 
                MetadataResolver = new Mono.Cecil.MetadataResolver(this) 
            });
        }

        public override Mono.Cecil.AssemblyDefinition Resolve(Mono.Cecil.AssemblyNameReference name, Mono.Cecil.ReaderParameters parameters)
        {
            Mono.Cecil.AssemblyDefinition def;
            if (cachedDefs.TryGetValue(name.Name, out def))
            {
                return def;
            }
            var result = base.Resolve(name, parameters);
            cachedDefs[name.Name] = result;
            return result;
        }
    }
}
