using Flame.Compiler;
using Flame.Compiler.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front.Target
{
    public class MultiBuildTargetParser : IBuildTargetParser
    {
        public MultiBuildTargetParser()
        {
            parsers = new List<IBuildTargetParser>();
        }

        private List<IBuildTargetParser> parsers;
        public IBuildTargetParser GetParser(string Identifier)
        {
            return parsers.FirstOrDefault(item => item.MatchesPlatformIdentifier(Identifier ?? ""));
        }
        public void RegisterParser(IBuildTargetParser Parser)
        {
            parsers.Add(Parser);
        }

        public IEnumerable<string> PlatformIdentifiers
        {
            get
            {
                return parsers.SelectMany(item => item.PlatformIdentifiers);
            }
        }

        public bool MatchesPlatformIdentifier(string Identifier)
        {
            return GetParser(Identifier) != null;
        }

        public IAssemblyResolver GetRuntimeAssemblyResolver(string Identifier)
        {
            return GetParser(Identifier).GetRuntimeAssemblyResolver(Identifier);
        }

        public BuildTarget CreateBuildTarget(string Identifier, IProject Project, ICompilerLog Log, IAssemblyResolver RuntimeAssemblyResolver, IAssemblyResolver ExternalResolver, PathIdentifier CurrentPath, PathIdentifier OutputDirectory)
        {
            return GetParser(Identifier).CreateBuildTarget(Identifier, Project, Log, RuntimeAssemblyResolver, ExternalResolver, CurrentPath, OutputDirectory);
        }
    }
}
